Imports System.IO
Imports OcrHexTools

Module bin2hex
    Sub Main(args As String())
        If args.Length = 0 Then
            File.WriteAllText("bin2hex.txt", OcrHexStreamCodec.GetInfo())
        Else
            AddHandler OcrHexStreamCodec.MessageOut, AddressOf MessageOut

            For Each arg In args
                Dim input = File.ReadAllBytes(arg)
                Dim outFilename = String.Format("{0}.ocrhex", arg)
                Dim out = OcrHexStreamCodec.Encode(input, outFilename)
                File.WriteAllBytes(outFilename, out)
            Next
        End If
    End Sub

    Private Sub MessageOut(msg As String)
        Console.Write(msg)
    End Sub
End Module
