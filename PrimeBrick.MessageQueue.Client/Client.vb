Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Security.Cryptography.X509Certificates
Imports System.Threading
Imports PrimeBrick.MessageQueue.Common

Public Class Client

    Public ReadOnly Property ServerAddress As String
    Public ReadOnly Property ServerPort As Integer
    Public ReadOnly Property UseSSL As Boolean

#Region "Events"
    Public Event OnClientLog(ByVal sender As Client, ByVal e As ClientLogEventArgs)

    Private Sub ClientLogHandler(EventArgs As ClientLogEventArgs)
        RaiseEvent OnClientLog(Me, EventArgs)
    End Sub
#End Region

#Region "Constructors"
    Public Sub New(ServerAddress As IPAddress, ServerPort As Integer, ByVal Optional UseSSL As Boolean = False)
        Me.ServerAddress = ServerAddress.ToString()
        Me.ServerPort = ServerPort
        Me.UseSSL = UseSSL
    End Sub

    Public Sub New(ServerAddress As String, ServerPort As Integer, ByVal Optional UseSSL As Boolean = False)
        Me.ServerAddress = ServerAddress
        Me.ServerPort = ServerPort
        Me.UseSSL = UseSSL
    End Sub

    Public Sub New(ServerEndPoint As IPEndPoint, ByVal Optional UseSSL As Boolean = False)
        Me.ServerAddress = ServerEndPoint.Address.ToString()
        Me.ServerPort = ServerEndPoint.Port
        Me.UseSSL = UseSSL
    End Sub
#End Region

    Private Function Serialize(ByRef value As Object) As Byte()
        Using MS As New MemoryStream
            Dim NS As New Common.NetworkSerializer()
            NS.Serialize(MS, value)
            Return MS.ToArray
        End Using
    End Function

    Public Function Publish(ByRef Content As Object)
        Try
            Dim NewMessage As New Message(MessageType.Publish, Content)
            Dim SerializedMessage = Serialize(NewMessage)

            Using Client As New TcpClient(Me.ServerAddress, Me.ServerPort)
                Console.WriteLine(String.Format("Connected to server {0}:{1}!", Me.ServerAddress, Me.ServerPort))
                Trace(String.Format("Connected to server {0}:{1}!", Me.ServerAddress, Me.ServerPort), LogGravity.Information)

                If UseSSL Then
                    WriteSecureStream(Client.GetStream(), SerializedMessage)
                Else
                    WriteStream(Client.GetStream(), SerializedMessage)
                End If

                Client.Close()
            End Using
        Catch ex As Exception
            Dim a = 0
        End Try

    End Function

    Private Sub WriteStream(ByRef Stream As Stream, ByRef Message As Byte())
        Using Buffer = New BufferedStream(Stream)
            Buffer.Write(Message, 0, Message.Length)
            Buffer.Flush()
        End Using
    End Sub

    Private Sub WriteSecureStream(ByRef Stream As Stream, ByRef Message As Byte())
        Using SecureStream = New SslStream(Stream, False) ', New RemoteCertificateValidationCallback(AddressOf RemoteCertificateValidation))
            SecureStream.AuthenticateAsClient("localhost")
            WriteStream(SecureStream, Message)
            SecureStream.Close()
        End Using
    End Sub
    Private Function RemoteCertificateValidation(sender As Object, certifcate As X509Certificate, chain As X509Chain, sslPolicy As SslPolicyErrors) As Boolean

        Return True
    End Function

    'Public Function Publish()

    '    Dim a = FastConverter.GetBytes(UShort.MaxValue)

    '    Dim q = New Queue()



    '    Dim Watch As New Stopwatch

    '    Dim TEST2 As New TESTSER2
    '    TEST2.Test1 = New DateTimeOffset()
    '    TEST2.Test2 = Guid.NewGuid

    '    Dim TEST1 As New TESTSER
    '    TEST1.Test1 = "CICICIIC"
    '    TEST1.Test2 = Integer.MaxValue
    '    TEST1.Test3 = Decimal.MaxValue
    '    TEST1.Test4 = True
    '    TEST1.Test5 = Now
    '    TEST1.Test6 = TEST2
    '    'TEST1.Test9 = Nothing

    '    Dim chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    '    Dim Random = New Random()
    '    Dim MessageCount = Integer.Parse(1000000)
    '    Dim str = ""
    '    For x = 1 To MessageCount
    '        Dim rndstr = New String(Enumerable.Repeat(chars, 1024).Select(Function(s) s(Random.Next(s.Length - 1))).ToArray())
    '        TEST1.Test7.Add(rndstr)
    '        'TEST1.Test10.Add(x, rndstr)
    '        'q.Enqueue(rndstr)
    '        ''str += rndstr
    '    Next





    '    'Watch.Reset()
    '    'Watch.Start()
    '    'For x = 1 To 100
    '    '    Dim c As Byte()
    '    '    Using mm As New MemoryStream

    '    '        Dim b = System.Text.Encoding.UTF8.GetBytes(str)
    '    '        Dim bb = BitConverter.GetBytes(b.Length)
    '    '        mm.Write(bb, 0, bb.Length)
    '    '        mm.Write(b, 0, b.Length)
    '    '        c = mm.ToArray
    '    '    End Using
    '    'Next
    '    'Watch.Stop()
    '    'Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())

    '    'Watch.Reset()
    '    'Watch.Start()
    '    'For x = 1 To 100
    '    '    Dim a As Byte()
    '    '    Using mm As New MemoryStream
    '    '        Dim bw As New BinaryWriter(mm)
    '    '        bw.Write(str)
    '    '        bw.Close()
    '    '        a = mm.ToArray

    '    '    End Using
    '    'Next
    '    'Watch.Stop()
    '    'Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())


    '    Watch.Reset()
    '    Watch.Start()
    '    Dim ba As Byte()
    '    Using m As New MemoryStream
    '        Dim w As New NetworkFormatter()
    '        w.Serialize(m, TEST1)
    '        ba = m.ToArray
    '    End Using
    '    Watch.Stop()
    '    Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())
    '    Watch.Start()
    '    Dim cust64 = Convert.ToBase64String(ba)
    '    Watch.Stop()
    '    Debug.WriteLine("custom to BASE64: " + Watch.ElapsedMilliseconds.ToString())
    '    Watch.Reset()
    '    Watch.Start()
    '    Dim bades As Object
    '    Using memorystream As New MemoryStream(ba)
    '        Dim w As New NetworkFormatter()
    '        bades = w.Deserialize(memorystream)
    '    End Using
    '    Watch.Stop()
    '    Debug.WriteLine("Custom DESER: " + Watch.ElapsedMilliseconds.ToString())


    '    Dim aa As Object = New Dynamic.ExpandoObject
    '    aa.pippo = "d"
    '    aa.pluto = 10D

    '    Watch.Reset()
    '    Watch.Start()
    '    Dim wiredoc As Byte()
    '    Dim wires As New Wire.Serializer()
    '    Using memorystream As New MemoryStream()
    '        wires.Serialize(aa, memorystream)
    '        wiredoc = memorystream.ToArray()
    '    End Using
    '    Watch.Stop()
    '    Debug.WriteLine("wire: " + Watch.ElapsedMilliseconds.ToString())
    '    Watch.Start()
    '    Dim wire64 = Convert.ToBase64String(wiredoc)
    '    Watch.Stop()
    '    Debug.WriteLine("wire to BASE64: " + Watch.ElapsedMilliseconds.ToString())
    '    Watch.Reset()
    '    Watch.Start()
    '    Dim wiredes As Object
    '    Using memorystream As New MemoryStream(wiredoc)
    '        wiredes = wires.Deserialize(memorystream)
    '    End Using
    '    Watch.Stop()
    '    Debug.WriteLine("wire DESER: " + Watch.ElapsedMilliseconds.ToString())


    '    Return ""
    'End Function

    '''' <summary>
    '''' Parse the string value passed as parameter into an object of type IPAddress.
    '''' It accept host name, ip address and the word 'Any' to bind on all available network interfaces.
    '''' </summary>
    '''' <param name="ValueToParse"></param>
    '''' <returns></returns>
    'Public Function ParseIPAddress(ByVal ValueToParse As String) As IPAddress
    '    If String.IsNullOrWhiteSpace(ValueToParse) Then Throw New ArgumentNullException(ValueToParse, "The value of the parameter cannot be empty.")

    '    If ValueToParse.ToLower = "any" Then
    '        Return IPAddress.Any
    '    Else
    '        Return Dns.GetHostAddresses(ValueToParse)(0)
    '    End If

    'End Function

    Private Sub Trace(Message As String, Gravity As LogGravity)
        Dim EventArgs As New ClientLogEventArgs(Message, Gravity)
        ThreadPool.QueueUserWorkItem(AddressOf ClientLogHandler, EventArgs)
    End Sub
End Class


'Public Class TESTSER
'    Public Property Test1 As String
'    Public Property Test2 As Integer
'    Public Property Test3 As Decimal
'    Public Property Test4 As Boolean
'    Public Property Test5 As DateTime
'    Public Property Test6 As Object
'    Public Property Test7 As List(Of String)
'    Private Property Test8 As String = "fffff"
'    Public Property Test9 As Integer?
'    Public Property Test10 As Dictionary(Of Integer, String)

'    Sub New()
'        Test7 = New List(Of String)
'        Test10 = New Dictionary(Of Integer, String)
'    End Sub
'End Class

'Public Class TESTSER2
'    Public Property Test1 As DateTimeOffset
'    Public Property Test2 As Guid
'End Class