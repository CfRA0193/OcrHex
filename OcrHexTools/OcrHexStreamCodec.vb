Imports System.Text

Public Module OcrHexStreamCodec
    Public AllSectorsMap As New Dictionary(Of Integer, Boolean)
    Public BadSectorsMap As New Dictionary(Of Integer, Boolean)

    Public Event MessageOut(msg As String)

    Public Function Encode(input As IEnumerable(Of Byte), Optional filename As String = "") As Byte()
        RaiseEvent MessageOut("bin2hex: CRC32...")
        Dim crc32hex = BitConverter.ToUInt32(CRC32.Process(input), 0).ToString("X2")
        If input.Count >= 65536 * OcrHexSector.PayloadByteSize Then
            Throw New Exception("Max input: 2 Mb!")
        End If
        Dim out As New StringBuilder()
        Dim bytes = New Queue(Of Byte)(input)
        Dim ocrSector, ocrSectorTest As OcrHexSector
        Dim ocrSectorString, ocrSectorString2 As String
        Dim sectorBytes As LinkedList(Of Byte)
        Dim nSectors = CInt(Math.Ceiling(bytes.Count / OcrHexSector.PayloadByteSize))

        Dim processSector = Sub(ocrSectorBytes As LinkedList(Of Byte), nSector As UInt16)
                                ocrSector = New OcrHexSector()
                                ocrSector.InitByPayload(ocrSectorBytes, nSector)
                                ocrSectorString = ocrSector.GetOcrHex()
                                With out
                                    .Append(String.Format("<{0}", nSector.ToString("D5")))
                                    .Append(ocrSectorString)
                                    .Append(vbCrLf)
                                End With
                                ocrSectorTest = New OcrHexSector()
                                If Not ocrSectorTest.InitByOcrHex(ocrSectorString) Then
                                    Throw New Exception("Not ocrSectorTest.InitByOcrHex(ocrSectorString)")
                                End If
                                ocrSectorString2 = ocrSectorTest.GetOcrHex()
                                If ocrSectorString <> ocrSectorString2 Then
                                    Throw New Exception("ocrSectorString <> ocrSectorString2")
                                End If
                            End Sub

        out.AppendLine("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\") '97 \
        out.AppendLine(String.Format("Filename:{0}, Length:{1}, CRC32:{2}", filename, input.Count, crc32hex))
        out.AppendLine("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\") '97 \
        For nSector As Integer = 0 To nSectors - 2
            sectorBytes = New LinkedList(Of Byte)()
            For i = 1 To OcrHexSector.PayloadByteSize
                sectorBytes.AddLast(bytes.Dequeue())
            Next
            processSector(sectorBytes, nSector)
            RaiseEvent MessageOut(String.Format("bin2hex: {0}%", (((nSector + 1) / nSectors) * 100).ToString("0.00")) + vbCr)
        Next
        sectorBytes = New LinkedList(Of Byte)()
        While bytes.Any()
            sectorBytes.AddLast(bytes.Dequeue())
        End While
        processSector(sectorBytes, nSectors - 1)
        out.AppendLine("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\") '97 \
        RaiseEvent MessageOut("bin2hex: 100%                  ")

        Return Encoding.ASCII.GetBytes(out.ToString())
    End Function

    Public Function Decode(parts As IEnumerable(Of IEnumerable(Of Byte))) As Byte()
        AllSectorsMap.Clear()
        Dim sectors As New Dictionary(Of Integer, Byte())
        For Each part In parts
            Dim chars = Encoding.ASCII.GetChars(part)
            Dim charsCorrected = New String(chars.ToArray())
            charsCorrected = charsCorrected.Replace("B"c, "3"c)
            charsCorrected = charsCorrected.Replace(")"c, ">"c)
            charsCorrected = charsCorrected.Replace("5"c, "S"c)
            charsCorrected = charsCorrected.Replace("Q"c, "9"c)
            charsCorrected = charsCorrected.Replace("2"c, "Z"c)
            charsCorrected = charsCorrected.Replace("6"c, "G"c)
            Dim ocrHex = New List(Of Char)(OcrHexByteCodec.Filter(charsCorrected, {">"c}).Select(Function(item) item))
            Dim idx = 0
            While idx < ocrHex.Count - 1
                Dim available = ocrHex.Count - idx
                Dim sectorCandidate = ocrHex.GetRange(idx, Math.Min(OcrHexSector.OcrHexCharSize, available))
                If OcrHexSector.PreambleDetected(sectorCandidate) Then
                    Dim ocrSector As New OcrHexSector()
                    If ocrSector.InitByOcrHex(sectorCandidate) Then
                        If Not sectors.ContainsKey(ocrSector.SectorNumber) Then
                            sectors.Add(ocrSector.SectorNumber, ocrSector.Payload)
                        End If
                        idx += OcrHexSector.OcrHexCharSize
                        RaiseEvent MessageOut(String.Format("hex2bin: {0}%", (((idx + 1) / ocrHex.Count) * 100).ToString("0.00")) + vbCr)
                    Else
                        idx += 1
                    End If
                Else
                    idx += 1
                End If
            End While
        Next
        RaiseEvent MessageOut("hex2bin: 100%          ")

        Dim out As New Queue(Of Byte)
        If sectors.Any() Then
            Dim maxSectorNumber = sectors.Keys.Max()
            For sectorNumber = 0 To maxSectorNumber
                If sectors.ContainsKey(sectorNumber) Then
                    For Each b In sectors(sectorNumber)
                        out.Enqueue(b)
                    Next
                    AllSectorsMap.Add(sectorNumber, True)
                Else
                    For i = 0 To OcrHexSector.PayloadByteSize
                        out.Enqueue(&H0)
                    Next
                    AllSectorsMap.Add(sectorNumber, False)
                    BadSectorsMap.Add(sectorNumber, False)
                End If
            Next
        End If

        Return out.ToArray()
    End Function

    Public Function GetAllSectorMap() As String
        Dim sb As New StringBuilder
        For Each kvp In AllSectorsMap
            sb.AppendLine(String.Format("<{0}> {1};", kvp.Key, If(kvp.Value, "OK", "BAD")))
        Next
        Return sb.ToString()
    End Function

    Public Function GetBadSectorMap() As String
        Dim sb As New StringBuilder
        For Each kvp In AllSectorsMap.Where(Function(item) item.Value = False)
            sb.AppendLine(String.Format("<{0}> {1};", kvp.Key, If(kvp.Value, "OK", "BAD")))
        Next
        Return sb.ToString()
    End Function

    Public Function GetInfo() As String
        Dim sb As New StringBuilder()
        With sb
            .AppendLine("==========================================================================")
            .AppendLine("OcrHex - bin to hex converter (OCR-friendly alphabet, CRC32 for every row)")
            .AppendLine("==========================================================================")
            .AppendLine("")
            .AppendLine("OcrHex ALPHABET (0..F): " + "#$+3479FGHLNSTXZ" + "; '>' - space symbol.")
            .AppendLine("Recommended font: 'Consolas', 9 pt.")
            .AppendLine("Recommended OCR:  'FreeOCR', grayscale mode.")
            .AppendLine("To repair damaged sector in text data, firstly find it on paper (by dec number).")
            .AppendLine("Following symbol's replacement is done automatically:")
            .AppendLine("'B' => '3' ')' => '>'  '5' => 'S'  'Q' => '9'  '2' => 'Z'  '6' => 'G'")
            .AppendLine("")
            .AppendLine("Sector hexadecimal form:")
            .AppendLine("<00000>11112222>33333333>...  ...>33333333>44444444>")
            .AppendLine("    0 - sector number (dec, big-endian);")
            .AppendLine("    1 - sector number, uint16 (hex, little-endian);")
            .AppendLine("    2 - payload size in bytes, uint16 (hex, little-endian);")
            .AppendLine("    3 - payload (hex), 4 bytes (hex, little-endian);")
            .AppendLine("    4 - CRC32 of 1..3 byte data, decoded from hex (without spaces!), uint32 (hex).")
            .AppendLine("")
            .AppendLine("''' <summary>")
            .AppendLine("''' CRC32 - WinZip, WinRAR, 7-Zip...")
            .AppendLine("''' </summary>")
            .AppendLine("Public Module CRC32")
            .AppendLine("    Public Function Process(data As IEnumerable(Of Byte)) As Byte()")
            .AppendLine("        Return Process(data, data.Count)")
            .AppendLine("    End Function")
            .AppendLine("    Public Function Process(data As IEnumerable(Of Byte), count As Integer) As Byte()")
            .AppendLine("        Dim i, j As Integer")
            .AppendLine("        Dim b, mask, crc32 As UInteger")
            .AppendLine("        crc32 = &HFFFFFFFFUI")
            .AppendLine("        For i = 0 To count - 1")
            .AppendLine("            b = data(i)")
            .AppendLine("            crc32 = crc32 Xor b")
            .AppendLine("            For j = 7 To 0 Step -1")
            .AppendLine("                mask = If(crc32 And 1, &HFFFFFFFFUI, 0)")
            .AppendLine("                crc32 = (crc32 >> 1) Xor (&HEDB88320UI And mask)")
            .AppendLine("            Next")
            .AppendLine("        Next")
            .AppendLine("        crc32 = Not crc32")
            .AppendLine("        Dim crc32b = New Byte((4) - 1) {}")
            .AppendLine("        crc32b(0) = CByte((crc32 And &HFF) >> 0)")
            .AppendLine("        crc32b(1) = CByte((crc32 And &HFF00) >> 8)")
            .AppendLine("        crc32b(2) = CByte((crc32 And &HFF0000) >> 16)")
            .AppendLine("        crc32b(3) = CByte((crc32 And &HFF000000UI) >> 24)")
            .AppendLine("        Return crc32b")
            .AppendLine("    End Function")
            .AppendLine("End Module")
            .AppendLine("")
            .AppendLine("CRC32 test (ASCII): 'The quick brown fox jumps over the lazy dog' - 414FA339")
            .AppendLine("")
        End With
        Return sb.ToString()
    End Function
End Module
