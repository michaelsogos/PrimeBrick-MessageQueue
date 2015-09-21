Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class Broker

    Private ConnectionWaitHandle As AutoResetEvent = New AutoResetEvent(False)
    Public ReadOnly Property ListenerAddress As IPAddress
    Public ReadOnly Property ListenerPort As Integer
    'Public ReadOnly Property ActiveConnections As Integer

#Region "Events"
    Public Event ServerLog(ByVal sender As Broker, ByVal e As ServerLogEventArgs)

    Private Sub ServerLogHandler(EventArgs As ServerLogEventArgs)
        RaiseEvent ServerLog(Me, EventArgs)
    End Sub
#End Region

#Region "Constructors"
    Public Sub New(ListenerAddress As IPAddress, ListenerPort As Integer)
        Me.ListenerAddress = ListenerAddress
        Me.ListenerPort = ListenerPort
    End Sub

    Public Sub New(ListenerAddress As String, ListenerPort As Integer)
        Me.ListenerAddress = ParseIPAddress(ListenerAddress)
        Me.ListenerPort = ListenerPort
    End Sub

    Public Sub New(ListenerEndPoint As IPEndPoint)
        Me.ListenerAddress = ListenerEndPoint.Address
        Me.ListenerPort = ListenerEndPoint.Port
    End Sub
#End Region

    ''' <summary>
    ''' Start the broker to listening on IP address and port specified
    ''' </summary>
    Public Sub Start()
        Dim Server As TcpListener = Nothing
        Try
            Server = New TcpListener(Me.ListenerAddress, Me.ListenerPort)
            Server.Start()
            RaiseEvent ServerLog(Me, New ServerLogEventArgs(String.Format("Broker started and listening on {0}:{1}", Me.ListenerAddress.ToString, Me.ListenerPort), LogGravity.Information))
            While True
                Dim Socket = Server.BeginAcceptSocket(New AsyncCallback(AddressOf HandleAsyncThread), Server)
                ConnectionWaitHandle.WaitOne()
                ConnectionWaitHandle.Reset()
            End While
        Catch ex As Exception
            Dim a = 0
        Finally
            If Server IsNot Nothing Then Server.Stop()
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

    Private Sub Trace(Message As String, Gravity As LogGravity)
        Dim EventArgs As New ServerLogEventArgs(Message, Gravity)
        ThreadPool.QueueUserWorkItem(AddressOf ServerLogHandler, EventArgs)
    End Sub

    Private Sub HandleAsyncThread(Result As IAsyncResult)
        'This is very useful because
        '   1. Give control on how many thread are available and permit an adjustment in case of easy server saturation, is better to scale out/up machine and not only threads
        '   2. Optimize application overhead reusing non-working threads
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf HandleSocketOperations), Result)
    End Sub

    Private Sub HandleSocketOperations(Result As IAsyncResult)
        Try
            Dim Server As TcpListener = Result.AsyncState
            Using Socket = Server.EndAcceptSocket(Result)
                ConnectionWaitHandle.Set()
                Dim RemoteEndPoint = Socket.RemoteEndPoint.ToString()
                Trace("Connected to " + RemoteEndPoint, LogGravity.Information)

                Dim SocketStream = New NetworkStream(Socket)
                Using Buffer = New BufferedStream(SocketStream)
                    While True
                        Dim DataBytesLength As Byte() = New Byte(3) {}
                        Buffer.Read(DataBytesLength, 0, 4)
                        Dim DataBytes As Byte() = New Byte(BitConverter.ToInt32(DataBytesLength, 0) - 1) {}
                        Buffer.Read(DataBytes, 0, DataBytes.Length)
                        Dim Data = New String(Encoding.Unicode.GetString(DataBytes))

                        If String.IsNullOrWhiteSpace(Data) Then Exit While

                        Dim Message = String.Format("Receive [{0}]", Data)
                        Dim MessageBytes = Encoding.UTF8.GetBytes(Message)
                        Dim MessageBytesLength = BitConverter.GetBytes(MessageBytes.Length)
                        Buffer.Write(MessageBytesLength, 0, 4)
                        Buffer.Write(MessageBytes, 0, MessageBytes.Length)
                        Buffer.Flush()
                    End While

                    Socket.Close()
                    Console.WriteLine("Disconnected from " + RemoteEndPoint)
                End Using
            End Using
        Catch ex As Exception
            Trace(String.Format("{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace), LogGravity.Error)
        End Try
    End Sub
End Class
