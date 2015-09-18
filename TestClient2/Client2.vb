Imports System.Configuration
Imports System.IO
Imports System.Net.Sockets
Imports System.Text

Module Client2

    Sub Main()

        Dim Messages As New List(Of String)
        Dim chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim Random = New Random()
        Dim MessageCount = Integer.Parse(ConfigurationManager.AppSettings("MessageCount"))
        For x = 1 To MessageCount
            Messages.Add(New String(Enumerable.Repeat(chars, ConfigurationManager.AppSettings("MessageSize")).Select(Function(s) s(Random.Next(s.Length - 1))).ToArray()))
        Next


        Dim Timer = New Stopwatch
        Timer.Start()
        Try
            Console.WriteLine("Connecting to SERVER ...")
            Using Client As New TcpClient(ConfigurationManager.AppSettings("ListenerHost"), ConfigurationManager.AppSettings("ListenerPort"))
                Console.WriteLine("Connected to the server!")

                Dim SocketStream = Client.GetStream()
                Using Buffer = New BufferedStream(SocketStream)

                    For Each Request In Messages
                        Dim Message = String.Format("CLIENT 2: [{0}]", Request)
                        Dim MessageBytes = Encoding.Unicode.GetBytes(Message)
                        Dim MessageBytesLength = BitConverter.GetBytes(MessageBytes.Length)
                        Buffer.Write(MessageBytesLength, 0, 4)
                        Buffer.Write(MessageBytes, 0, MessageBytes.Length)
                        Buffer.Flush()

                        Dim DataBytesLength As Byte() = New Byte(3) {}
                        Buffer.Read(DataBytesLength, 0, 4)
                        Dim DataBytes As Byte() = New Byte(BitConverter.ToInt32(DataBytesLength, 0) - 1) {}
                        Buffer.Read(DataBytes, 0, DataBytes.Length)
                        Dim Response = New String(Encoding.UTF8.GetString(DataBytes))
                        If String.IsNullOrWhiteSpace(Response) Then Continue For
                        'Console.WriteLine(Response)
                    Next

                    Client.Close()
                End Using
            End Using
        Catch ex As Exception
            Dim a = 0
        End Try
        Timer.Stop()
        Console.WriteLine("Elapsed: " + Timer.ElapsedMilliseconds.ToString)
        Console.ReadLine()
    End Sub

End Module
