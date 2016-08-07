Imports PrimeBrick.MessageQueue.Server

Module Broker

    'Private ConnectionWaitHandle As AutoResetEvent = New AutoResetEvent(False)
    'Private MaxThreads As Integer
    'Private MaxAsyncThread As Integer

    Sub main()
        Dim Server As New Listener("any", 50605)
        AddHandler Server.OnServerLog, AddressOf OnServerLog
        AddHandler Server.OnReceiveMessage, AddressOf OnReceiveMessage
        Server.Start()

        Dim ServerCertificate = New SecureServerConfiguration(Security.Authentication.SslProtocols.Tls, "C:\temp\test.pfx", "50605")
        Dim ServerSSL As New Listener("any", 50606, ServerCertificate)
        AddHandler ServerSSL.OnServerLog, AddressOf OnServerLog
        AddHandler ServerSSL.OnReceiveMessage, AddressOf OnReceiveMessage
        ServerSSL.Start()

        Console.WriteLine("Press a key")
        Console.ReadKey()
        Server.Stop()
        ServerSSL.Stop()
    End Sub

    Private Sub OnServerLog(sender As Listener, e As ServerLogEventArgs)
        Console.WriteLine(e.Message)
    End Sub

    Private Sub OnReceiveMessage(sender As Listener, e As ReceivedMessageArgs)
        Console.WriteLine(e.Message.Content)
    End Sub

    'Sub Main()
    '    Dim BufferedServer As TcpListener = Nothing
    '    Dim NotBufferedServer As TcpListener = Nothing
    '    ThreadPool.GetMaxThreads(MaxThreads, MaxAsyncThread)
    '    'ThreadPool.SetMaxThreads(MaxThreads, 1000)

    '    Try
    '        Dim BufferedListenerIPAddress As IPAddress
    '        Dim BufferedListenerHost As String = ConfigurationManager.AppSettings("BufferedListenerHost")
    '        Dim BufferedListenerPort As String = ConfigurationManager.AppSettings("BufferedListenerPort")
    '        Dim BufferedListenerIPPort As Integer
    '        If String.IsNullOrWhiteSpace(BufferedListenerHost) Then Throw New NullReferenceException("The app settings parameter [BufferedListenerHost] cannot be empty!")
    '        If String.IsNullOrWhiteSpace(BufferedListenerPort) Then Throw New NullReferenceException("The app settings parameter [BufferedListenerPort] cannot be empty!")
    '        If Not Integer.TryParse(BufferedListenerPort, BufferedListenerIPPort) Then Throw New FormatException("The app settings parameter [BufferedListenerPort] value have to be a valid number between 1 and 65535!")
    '        If BufferedListenerHost.ToLower = "any" Then
    '            BufferedListenerIPAddress = IPAddress.Any
    '        Else
    '            BufferedListenerIPAddress = Dns.GetHostAddresses(BufferedListenerHost)(0)
    '        End If
    '        BufferedServer = New TcpListener(BufferedListenerIPAddress, BufferedListenerIPPort)
    '        BufferedServer.Start()


    '        Dim NotBufferedListenerIPAddress As IPAddress
    '        Dim NotBufferedListenerHost As String = ConfigurationManager.AppSettings("NotBufferedListenerHost")
    '        Dim NotBufferedListenerPort As String = ConfigurationManager.AppSettings("NotBufferedListenerPort")
    '        Dim NotBufferedListenerIPPort As Integer
    '        If String.IsNullOrWhiteSpace(NotBufferedListenerHost) Then Throw New NullReferenceException("The app settings parameter [NotBufferedListenerHost] cannot be empty!")
    '        If String.IsNullOrWhiteSpace(NotBufferedListenerPort) Then Throw New NullReferenceException("The app settings parameter [NotBufferedListenerPort] cannot be empty!")
    '        If Not Integer.TryParse(NotBufferedListenerPort, NotBufferedListenerIPPort) Then Throw New FormatException("The app settings parameter [NotBufferedListenerPort] value have to be a valid number between 1 and 65535!")
    '        If NotBufferedListenerHost.ToLower = "any" Then
    '            NotBufferedListenerIPAddress = IPAddress.Any
    '        Else
    '            NotBufferedListenerIPAddress = Dns.GetHostAddresses(NotBufferedListenerHost)(0)
    '        End If
    '        NotBufferedServer = New TcpListener(NotBufferedListenerIPAddress, NotBufferedListenerIPPort)
    '        NotBufferedServer.Start()


    '        While True
    '            Console.WriteLine("Waiting for a connection... ")
    '            Dim BufferedSocket = BufferedServer.BeginAcceptSocket(New AsyncCallback(AddressOf HandleAsyncBufferedThread), BufferedServer)
    '            Dim NotBufferedSocket = NotBufferedServer.BeginAcceptSocket(New AsyncCallback(AddressOf HandleAsyncNotBufferedThread), NotBufferedServer)
    '            ConnectionWaitHandle.WaitOne()
    '            ConnectionWaitHandle.Reset()
    '        End While
    '    Catch ex As Exception
    '        Dim a = 0
    '    Finally
    '        If BufferedServer IsNot Nothing Then BufferedServer.Stop()
    '        If NotBufferedServer IsNot Nothing Then NotBufferedServer.Stop()
    '    End Try
    'End Sub

    'Private Sub HandleAsyncNotBufferedThread(Result As IAsyncResult)
    '    'This is very useful because
    '    '   1. Give control on how many thread are available and permit an adjustment in case of easy server saturation, is better to scale out/up machine and not only threads
    '    '   2. Optimize application overhead reusing non-working threads
    '    ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf HandleNotBufferedSocketOperations), Result)
    'End Sub

    'Private Sub HandleAsyncBufferedThread(Result As IAsyncResult)
    '    'This is very useful because
    '    '   1. Give control on how many thread are available and permit an adjustment in case of easy server saturation, is better to scale out/up machine and not only threads
    '    '   2. Optimize application overhead reusing non-working threads
    '    ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf HandleBufferedSocketOperations), Result)
    'End Sub

    'Private Sub HandleNotBufferedSocketOperations(Result As IAsyncResult)
    '    Console.WriteLine("Connected!")
    '    Dim Server As TcpListener = Result.AsyncState
    '    Using Socket = Server.EndAcceptSocket(Result)
    '        ConnectionWaitHandle.Set()
    '        Dim RemoteEndPoint As String = Socket.RemoteEndPoint.ToString()

    '        Console.WriteLine("NOT BUFFERED Socket open on tcp connection " + RemoteEndPoint)

    '        Dim SocketStream = New NetworkStream(Socket)
    '        Dim SR = New StreamReader(SocketStream)
    '        Dim SW = New StreamWriter(SocketStream)

    '        While True
    '            Dim Data = SR.ReadLine()
    '            If String.IsNullOrWhiteSpace(Data) Then Exit While
    '            'Console.WriteLine(Data)
    '            SW.WriteLine("Receive [" + Data + "]")
    '            SW.Flush()
    '        End While

    '        Socket.Close()
    '        Console.WriteLine("Socket closed on tcp connection " + RemoteEndPoint)
    '    End Using
    'End Sub

    'Private Sub HandleBufferedSocketOperations(Result As IAsyncResult)
    '    Console.WriteLine("Connected!")
    '    Dim Server As TcpListener = Result.AsyncState

    '    Using Socket = Server.EndAcceptSocket(Result)
    '        ConnectionWaitHandle.Set()
    '        Dim RemoteEndPoint As String = Socket.RemoteEndPoint.ToString()

    '        Console.WriteLine("BUFFERED Socket open on tcp connection " + RemoteEndPoint)

    '        Dim SocketStream = New NetworkStream(Socket)
    '        Using Buffer = New BufferedStream(SocketStream)

    '            While True
    '                Dim DataBytesLength As Byte() = New Byte(3) {}
    '                Buffer.Read(DataBytesLength, 0, 4)
    '                Dim DataBytes As Byte() = New Byte(BitConverter.ToInt32(DataBytesLength, 0) - 1) {}
    '                Buffer.Read(DataBytes, 0, DataBytes.Length)
    '                Dim Data = New String(Encoding.Unicode.GetString(DataBytes))

    '                If String.IsNullOrWhiteSpace(Data) Then Exit While

    '                'Console.WriteLine(Data)
    '                Dim Message = String.Format("Receive [{0}]", Data)
    '                Dim MessageBytes = Encoding.UTF8.GetBytes(Message)
    '                Dim MessageBytesLength = BitConverter.GetBytes(MessageBytes.Length)
    '                Buffer.Write(MessageBytesLength, 0, 4)
    '                Buffer.Write(MessageBytes, 0, MessageBytes.Length)
    '                Buffer.Flush()
    '            End While

    '            Socket.Close()
    '            Console.WriteLine("Socket closed on tcp connection " + RemoteEndPoint)
    '        End Using
    '    End Using
    'End Sub


End Module
