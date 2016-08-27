Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Threading
Imports PrimeBrick.MessageQueue.Common

Public Class Listener

    Private ConnectionWaitHandle As AutoResetEvent = New AutoResetEvent(False)
    Public ReadOnly Property ListenerAddress As IPAddress
    Public ReadOnly Property ListenerPort As Integer
    Public ReadOnly Property SecureServerConfiguration As SecureServerConfiguration
    Private Server As TcpListener = Nothing
    Private ServerListening As Boolean = False

    'Public ReadOnly Property ActiveConnections As Integer

#Region "Events"
    Public Event OnServerLog(ByVal sender As Listener, ByVal e As ServerLogEventArgs)
    Public Event OnReceiveMessage(ByVal sender As Listener, ByVal e As ReceivedMessageArgs)

    Private Sub ServerLogHandler(EventArgs As ServerLogEventArgs)
        RaiseEvent OnServerLog(Me, EventArgs)
    End Sub

    Private Sub ReceivedMessageHandler(EventArgs As ReceivedMessageArgs)
        RaiseEvent OnReceiveMessage(Me, EventArgs)
    End Sub
#End Region

#Region "Constructors"
    Public Sub New(ListenerAddress As IPAddress, ListenerPort As Integer, Optional ByVal SecureServerConfiguration As SecureServerConfiguration = Nothing)
        Me.ListenerAddress = ListenerAddress
        Me.ListenerPort = ListenerPort
        Me.SecureServerConfiguration = SecureServerConfiguration
    End Sub

    Public Sub New(ListenerAddress As String, ListenerPort As Integer, Optional ByVal SecureServerConfiguration As SecureServerConfiguration = Nothing)
        Me.ListenerAddress = ParseIPAddress(ListenerAddress)
        Me.ListenerPort = ListenerPort
        Me.SecureServerConfiguration = SecureServerConfiguration
    End Sub

    Public Sub New(ListenerEndPoint As IPEndPoint, Optional ByVal SecureServerConfiguration As SecureServerConfiguration = Nothing)
        Me.ListenerAddress = ListenerEndPoint.Address
        Me.ListenerPort = ListenerEndPoint.Port
        Me.SecureServerConfiguration = SecureServerConfiguration
    End Sub
#End Region


    ''' <summary>
    ''' Start the broker to listening on IP address and port specified
    ''' </summary>
    Public Sub Start()
        Server = New TcpListener(Me.ListenerAddress, Me.ListenerPort)
        Server.Start()
        ServerListening = True
        ServerLogHandler(New ServerLogEventArgs(String.Format("Broker started and listening on {0}:{1}", Me.ListenerAddress.ToString, Me.ListenerPort), LogSeverity.Information))
        ServerLogHandler(New ServerLogEventArgs(String.Format("Broker IS {0}SECURE", If(SecureServerConfiguration Is Nothing, "NOT ", "")), LogSeverity.Information))
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf HandleAsyncTcpListener))
    End Sub

    Public Sub [Stop]()
        Try
            ServerListening = False
        Catch ex As Exception
            Dim a = 0
        End Try
    End Sub

    ''' <summary>
    ''' Parse the string value passed as parameter into an object of type IPAddress.
    ''' It accept host name, ip address and the word 'Any' to bind on all available network interfaces.
    ''' </summary>
    ''' <param name="ValueToParse"></param>
    ''' <returns></returns>
    Public Function ParseIPAddress(ByVal ValueToParse As String) As IPAddress
        If String.IsNullOrWhiteSpace(ValueToParse) Then Throw New ArgumentNullException(ValueToParse, "The value of the parameter cannot be empty.")

        If ValueToParse.ToLower = "any" Then
            Return IPAddress.Any
        Else
            Return Dns.GetHostAddresses(ValueToParse)(0)
        End If

    End Function

    Private Sub Trace(Message As String, Severity As LogSeverity)
        Dim EventArgs As New ServerLogEventArgs(Message, Severity)
        ThreadPool.QueueUserWorkItem(AddressOf ServerLogHandler, EventArgs)
    End Sub

    Private Sub Receive(Message As Message)
        Dim EventArgs As New ReceivedMessageArgs(Message)
        ThreadPool.QueueUserWorkItem(AddressOf ReceivedMessageHandler, EventArgs)
    End Sub

    Private Sub HandleAsyncTcpListener()
        Try
            ConnectionWaitHandle.Set()
            While ServerListening
                Dim Socket = Server.BeginAcceptTcpClient(New AsyncCallback(AddressOf HandleAsyncTcpClient), Server)
                ConnectionWaitHandle.WaitOne()
                ConnectionWaitHandle.Reset()
            End While
        Catch ex As Exception
            Dim a = 0
        Finally
            If Server IsNot Nothing Then Server.Stop()
            ConnectionWaitHandle.Close()
        End Try
    End Sub

    Private Sub HandleAsyncTcpClient(ServerAsync As IAsyncResult)
        'This is very useful because
        '   1. Give control on how many thread are available and permit an adjustment in case of easy server saturation, is better to scale out/up machine and not only threads
        '   2. Optimize application overhead reusing non-working threads
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf HandleTcpClientOperations), ServerAsync)
    End Sub

    Private Sub HandleTcpClientOperations(ServerAsync As IAsyncResult)
        Try
            If Not ServerListening Then Return
            Using Socket = Server.EndAcceptTcpClient(ServerAsync)
                ConnectionWaitHandle.Set()
                Dim RemoteEndPoint = Socket.Client.RemoteEndPoint.ToString()
                Trace(String.Format("Connected to {0}", RemoteEndPoint), LogSeverity.Information)

                If SecureServerConfiguration IsNot Nothing Then
                    ReadSecureStream(Socket.GetStream())
                Else
                    ReadStream(Socket.GetStream())
                End If

                Socket.Close()

                Trace(String.Format("Disconnected from {0}", RemoteEndPoint), LogSeverity.Information)
                'End Using
            End Using
        Catch ex As Exception
            Trace(String.Format("{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace), LogSeverity.Error)
        End Try
    End Sub

    Private Sub ReadStream(ByRef Stream As Stream)
        Using MS As New MemoryStream()
            'Dim SocketStream = New NetworkStream(Socket)
            'Using Buffer = New BufferedStream(SocketStream)
            'While True
            'Dim DataBytesLength As Byte() = New Byte(3) {}
            'Buffer.Read(DataBytesLength, 0, 4)
            'Dim DataBytes As Byte() = New Byte(BitConverter.ToInt32(DataBytesLength, 0) - 1) {}
            'Buffer.Read(DataBytes, 0, DataBytes.Length)



            'Dim Data = New String(Encoding.Unicode.GetString(DataBytes))

            'If String.IsNullOrWhiteSpace(DataBytes.Length <= 0) Then Exit While

            'Dim Message = String.Format("Receive [{0}]", Data)
            'Dim MessageBytes = Encoding.UTF8.GetBytes(Message)
            'Dim MessageBytesLength = BitConverter.GetBytes(MessageBytes.Length)
            'Buffer.Write(MessageBytesLength, 0, 4)
            'Buffer.Write(MessageBytes, 0, MessageBytes.Length)
            'Buffer.Flush()

            'End While


            Stream.CopyTo(MS)
            MS.Position = 0
            Dim NS As New NetworkSerializer()
            Dim Content = NS.Deserialize(Of Message)(MS)
            Receive(Content)
        End Using
    End Sub

    Private Sub ReadSecureStream(ByRef Stream As Stream)
        Using SecureStream As New SslStream(Stream, False) ', New RemoteCertificateValidationCallback(AddressOf RemoteCertificateValidation))
            SecureStream.AuthenticateAsServer(SecureServerConfiguration.Certificate, False, SecureServerConfiguration.SecureProtocol, True)

            'DisplaySecurityLevel(SecureStream)
            'DisplaySecurityServices(SecureStream)
            'DisplayCertificateInformation(SecureStream)
            'DisplayStreamProperties(SecureStream)

            ReadStream(SecureStream)
            SecureStream.Close()
        End Using
    End Sub

#Region "Certificate Validation"
    'Private Function RemoteCertificateValidation(sender As Object, certifcate As X509Certificate, chain As X509Chain, sslPolicy As SslPolicyErrors) As Boolean

    '    Return True
    'End Function

    'Private Sub DisplaySecurityLevel(stream As SslStream)
    '    Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength)
    '    Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength)
    '    Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength)
    '    Console.WriteLine("Protocol: {0}", stream.SslProtocol)
    'End Sub

    'Private Sub DisplaySecurityServices(stream As SslStream)
    '    Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer)
    '    Console.WriteLine("IsSigned: {0}", stream.IsSigned)
    '    Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted)
    'End Sub

    'Private Sub DisplayStreamProperties(stream As SslStream)
    '    Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite)
    '    Console.WriteLine("Can timeout: {0}", stream.CanTimeout)
    'End Sub

    'Private Sub DisplayCertificateInformation(stream As SslStream)
    '    Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus)

    '    Dim localCertificate = stream.LocalCertificate
    '    If localCertificate IsNot Nothing Then
    '        Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.", localCertificate.Subject, localCertificate.GetEffectiveDateString(), localCertificate.GetExpirationDateString())

    '    Else
    '        Console.WriteLine("Local certificate is null.")
    '    End If

    '    ' Display the properties of the client's certificate.
    '    Dim remoteCertificate = stream.RemoteCertificate
    '    If (remoteCertificate IsNot Nothing) Then
    '        Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.", remoteCertificate.Subject, remoteCertificate.GetEffectiveDateString(), remoteCertificate.GetExpirationDateString())
    '    Else
    '        Console.WriteLine("Remote certificate is null.")
    '    End If
    'End Sub
#End Region

End Class
