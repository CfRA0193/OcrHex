Imports System.IO
Imports System.Text
Imports OcrHexTools

Module hex2bin
    Sub Main(args As String())
        If args.Length = 0 Then
            File.WriteAllText("hex2bin.txt", OcrHexStreamCodec.GetInfo())
        Else
            AddHandler OcrHexStreamCodec.MessageOut, AddressOf MessageOut

            Dim parts As New List(Of Byte())
            For Each arg In args
                Dim text = File.ReadAllText(arg)
                Dim textBytes = Encoding.ASCII.GetBytes(text)
                parts.Add(textBytes)
            Next

            Dim out = OcrHexStreamCodec.Decode(parts)
            Dim outName = args(0) + ".bin"
            File.WriteAllBytes(outName, out.ToArray())

            File.WriteAllText(args(0) + ".bin.allsectormap", OcrHexStreamCodec.GetAllSectorMap())
            File.WriteAllText(args(0) + ".bin.badsectormap", OcrHexStreamCodec.GetBadSectorMap())
        End If
    End Sub

    Private Sub MessageOut(msg As String)
        Console.Write(msg)
    End Sub
End Module
