Imports System.Text

Public Module OcrHexByteCodec
    Private _enc As String = "#$+3479FGHLNSTXZ" '#$+3479FGHLNSTXZ
    Private _dec As New Dictionary(Of Char, Byte) From {{_enc(&H0), &H0}, {_enc(&H1), &H1}, {_enc(&H2), &H2}, {_enc(&H3), &H3},
                                                        {_enc(&H4), &H4}, {_enc(&H5), &H5}, {_enc(&H6), &H6}, {_enc(&H7), &H7},
                                                        {_enc(&H8), &H8}, {_enc(&H9), &H9}, {_enc(&HA), &HA}, {_enc(&HB), &HB},
                                                        {_enc(&HC), &HC}, {_enc(&HD), &HD}, {_enc(&HE), &HE}, {_enc(&HF), &HF}}

    Public Function OctetIsValid(octetValue As Char) As Boolean
        Return _dec.ContainsKey(octetValue)
    End Function

    Public Function Encode(hexValue As Byte) As Char()
        Return {_enc(hexValue And &HF), _enc((hexValue >> 4) And &HF)}
    End Function

    Public Function Decode(octets As Char()) As Byte
        If _dec.ContainsKey(octets(0)) AndAlso _dec.ContainsKey(octets(1)) Then
            Return _dec(octets(0)) Or ((_dec(octets(1)) << 4) And &HF0)
        Else
            Throw New Exception("OcrHexByteCodec::Decode(): Octet is out of range")
        End If
    End Function

    Public Function Filter(ocrHex As IEnumerable(Of Char), excluded As IEnumerable(Of Char)) As String
        Return New String(ocrHex.Where(Function(item) excluded.Contains(item) OrElse OcrHexByteCodec.OctetIsValid(item)).ToArray())
    End Function
End Module
