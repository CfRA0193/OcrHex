Imports System.Text

Public Class OcrHexSector
    Public Const SpacesInSector = 19
    Public Const ByteSpacing = 4
    Public Const PayloadByteSize = 32
    Public Const OcrHexCharSize = 91
    Public Const OcrHexCharSizeNoSpaces = 80
    Public ReadOnly Property Bytes As New List(Of Byte)({0, 0, 0, 0})

    Public Property SectorNumber As UInt16
        Get
            Return BitConverter.ToUInt16(_Bytes.GetRange(0, 2).ToArray(), 0)
        End Get
        Set(value As UInt16)
            Dim bytes = BitConverter.GetBytes(value)
            For i = 0 To bytes.Length - 1
                _Bytes(i) = bytes(i)
            Next
        End Set
    End Property

    Public ReadOnly Property PayloadLength As UInt16
        Get
            Return BitConverter.ToUInt16(_Bytes.GetRange(2, 2).ToArray(), 0)
        End Get
    End Property

    Public ReadOnly Property Payload As Byte()
        Get
            Return _Bytes.GetRange(4, PayloadLength).ToArray()
        End Get
    End Property

    Public ReadOnly Property Crc32UI() As UInt32
        Get
            Return BitConverter.ToUInt32(_Bytes.GetRange(_Bytes.Count - 4, 4).ToArray(), 0)
        End Get
    End Property

    Public Shared Function PreambleDetected(ocrHex As IEnumerable(Of Char)) As Boolean
        Dim spacingRemain = 0
        For i = 0 To ocrHex.Count - 1
            If spacingRemain = 0 Then
                If ocrHex(i) <> ">"c Then
                    Return False
                End If
                spacingRemain = ByteSpacing * 2
            Else
                If ocrHex(i) = ">"c Then
                    Return False
                End If
                spacingRemain -= 1
            End If
        Next
        Return True
    End Function

    Public Function CheckCRC32() As Boolean
        Dim crc32Current = BitConverter.ToUInt32(CRC32.Process(_Bytes, _Bytes.Count - 4), 0)
        Dim crc32Stored = Crc32UI()
        Return crc32Current = crc32Stored
    End Function

    Public Sub InitByPayload(payload As IEnumerable(Of Byte), nSector As UInt16)
        Me.SectorNumber = nSector
        With _Bytes
            .Clear()
            .AddRange(BitConverter.GetBytes(nSector))
            .AddRange(BitConverter.GetBytes(Convert.ToUInt16(payload.Count)))
            .AddRange(payload)
            .AddRange(CRC32.Process(_Bytes))
        End With
    End Sub

    Public Function InitByOcrHex(ocrHex As IEnumerable(Of Char)) As Boolean
        Dim filtered = OcrHexByteCodec.Filter(ocrHex, {})
        If filtered.Length >= 8 Then
            Dim workLength = Math.Min(filtered.Length - filtered.Length Mod 2, OcrHexCharSizeNoSpaces)
            _Bytes.Clear()
            For i = 0 To workLength - 1 Step 2
                If OcrHexByteCodec.OctetIsValid(filtered(i)) AndAlso OcrHexByteCodec.OctetIsValid(filtered(i + 1)) Then
                    Dim b = OcrHexByteCodec.Decode({filtered(i), filtered(i + 1)})
                    _Bytes.Add(b)
                End If
            Next
            Return CheckCRC32()
        Else
            Return False
        End If
    End Function

    Public Function GetOcrHex() As String
        Dim result As New StringBuilder()
        result.Append(">")
        Dim spacingRemain = ByteSpacing
        For Each b In _Bytes
            result.Append(OcrHexByteCodec.Encode(b))
            spacingRemain -= 1
            If spacingRemain = 0 Then
                spacingRemain = ByteSpacing
                result.Append(">")
            End If
        Next
        While result.Length < OcrHexCharSize
            result.Append("\")
        End While
        Return result.ToString()
    End Function
End Class
