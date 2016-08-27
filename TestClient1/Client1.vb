Imports PrimeBrick.MessageQueue.Client

Module Client1

    Class TESTEX
        Public Property GHE As Double
        Public Property lol As Guid
    End Class

    Sub Main()
        Dim Publisher As New Client("localhost", 50605)
        AddHandler Publisher.OnClientLog, AddressOf OnClientLog
        'Dim SSLClient As New PrimeBrick.MessageQueue.Client.Client("localhost", 50606, True)
        'SSLClient.Publish("CIAO A TUTTI :)")
        'Console.WriteLine("Press a key")
        'Console.ReadKey()

        'Client.Publish(New With {.P1 = "ciao", .p2 = 133})
        'Console.WriteLine("Press a key")
        'Console.ReadKey()

        Publisher.Publish("TESTMQ", New TESTEX With {.GHE = 1.4, .lol = Guid.NewGuid})
        Console.WriteLine("Press a key")
        Console.ReadKey()
    End Sub

    Private Sub OnClientLog(sender As Client, e As ClientLogEventArgs)
        Console.WriteLine(String.Format("{1}{0}{2}{0}{3}", vbTab, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.fff"), e.Severity.ToString().ToUpper(), e.Message))
    End Sub



    'Sub Main()

    '    Dim Messages As New List(Of String)
    '    Dim chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    '    Dim Random = New Random()
    '    Dim MessageCount = Integer.Parse(ConfigurationManager.AppSettings("MessageCount"))
    '    For x = 1 To MessageCount
    '        Messages.Add(New String(Enumerable.Repeat(chars, ConfigurationManager.AppSettings("MessageSize")).Select(Function(s) s(Random.Next(s.Length - 1))).ToArray()))
    '    Next

    '    Dim Timer = New Stopwatch
    '    Timer.Start()
    '    Try
    '        Console.WriteLine("Connecting to SERVER ...")
    '        Using Client As New TcpClient(ConfigurationManager.AppSettings("ListenerHost"), ConfigurationManager.AppSettings("ListenerPort"))
    '            Console.WriteLine("Connected to the server!")

    '            Dim SocketStream = Client.GetStream()
    '            Dim SR = New StreamReader(SocketStream)
    '            Dim SW = New StreamWriter(SocketStream)

    '            For Each Request In Messages
    '                SW.WriteLine("CLIENT 1: [" + Request + "]")
    '                SW.Flush()

    '                Dim Response = SR.ReadLine()
    '                If String.IsNullOrWhiteSpace(Response) Then Exit For
    '                'Console.WriteLine(Response)
    '            Next

    '            Client.Close()
    '        End Using
    '    Catch ex As Exception
    '        Dim a = 0
    '    End Try
    '    Timer.Stop()
    '    Console.WriteLine("Elapsed: " + Timer.ElapsedMilliseconds.ToString)
    '    Console.ReadLine()
    'End Sub

End Module
