﻿Imports System.Configuration
Imports System.IO
Imports System.Net.Sockets
Imports System.Text

Module Client1

    Sub Main()
        Dim a As New PrimeBrick.MessageQueue.Client.Client("localhost", 50605)
        a.Publish()
        Console.ReadKey()
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
