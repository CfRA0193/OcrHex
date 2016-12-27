''' <summary>
''' CRC32 - WinZip, WinRAR, 7-Zip...
''' </summary>
''' <remarks>
''' CRC32 test (ASCII): 'The quick brown fox jumps over the lazy dog' - 414FA339
''' </remarks>
Public Module CRC32
    Public Function Process(data As IEnumerable(Of Byte)) As Byte()
        Return Process(data, data.Count)
    End Function

    Public Function Process(data As IEnumerable(Of Byte), count As Integer) As Byte()
        Dim i, j As Integer
        Dim b, mask, crc32 As UInteger
        crc32 = &HFFFFFFFFUI
        For i = 0 To count - 1
            b = data(i)
            crc32 = crc32 Xor b
            For j = 7 To 0 Step -1
                mask = If(crc32 And 1, &HFFFFFFFFUI, 0)
                crc32 = (crc32 >> 1) Xor (&HEDB88320UI And mask)
            Next
        Next
        crc32 = Not crc32
        Dim crc32b = New Byte((4) - 1) {}
        crc32b(0) = CByte((crc32 And &HFF) >> 0)
        crc32b(1) = CByte((crc32 And &HFF00) >> 8)
        crc32b(2) = CByte((crc32 And &HFF0000) >> 16)
        crc32b(3) = CByte((crc32 And &HFF000000UI) >> 24)
        Return crc32b
    End Function
End Module
