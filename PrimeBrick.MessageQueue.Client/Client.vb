Imports System.IO
Imports System.Net
Imports System.Runtime.Serialization.Formatters.Binary
Imports MsgPack
Imports MsgPack.Serialization
Imports Newtonsoft.Json

Public Class Client

    Public ReadOnly Property ServerAddress As IPAddress
    Public ReadOnly Property ServerPort As Integer

#Region "Constructors"
    Public Sub New(ServerAddress As IPAddress, ServerPort As Integer)
        Me.ServerAddress = ServerAddress
        Me.ServerPort = ServerPort
    End Sub

    Public Sub New(ServerAddress As String, ServerPort As Integer)
        Me.ServerAddress = ParseIPAddress(ServerAddress)
        Me.ServerPort = ServerPort
    End Sub

    Public Sub New(ServerEndPoint As IPEndPoint)
        Me.ServerAddress = ServerEndPoint.Address
        Me.ServerPort = ServerEndPoint.Port
    End Sub
#End Region

    Public Function Publish()

        Dim Watch As New Stopwatch
        Dim TEST2 As New TESTSER2
        TEST2.Test1 = New DateTimeOffset()
        TEST2.Test2 = Guid.NewGuid

        Dim TEST1 As New TESTSER
        TEST1.Test1 = "CICICIIC"
        TEST1.Test2 = Integer.MaxValue
        TEST1.Test3 = Decimal.MaxValue
        TEST1.Test4 = True
        TEST1.Test5 = Now
        TEST1.Test6 = TEST2
        'TEST1.Test9 = Nothing

        Dim chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim Random = New Random()
        Dim MessageCount = Integer.Parse(1000)
        Dim str = ""
        For x = 1 To MessageCount
            Dim rndstr = New String(Enumerable.Repeat(chars, 1024).Select(Function(s) s(Random.Next(s.Length - 1))).ToArray())
            TEST1.Test7.Add(rndstr)
            str += rndstr
        Next



        'Watch.Reset()
        'Watch.Start()
        'For x = 1 To 100
        '    Dim c As Byte()
        '    Using mm As New MemoryStream

        '        Dim b = System.Text.Encoding.UTF8.GetBytes(str)
        '        Dim bb = BitConverter.GetBytes(b.Length)
        '        mm.Write(bb, 0, bb.Length)
        '        mm.Write(b, 0, b.Length)
        '        c = mm.ToArray
        '    End Using
        'Next
        'Watch.Stop()
        'Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())

        'Watch.Reset()
        'Watch.Start()
        'For x = 1 To 100
        '    Dim a As Byte()
        '    Using mm As New MemoryStream
        '        Dim bw As New BinaryWriter(mm)
        '        bw.Write(str)
        '        bw.Close()
        '        a = mm.ToArray

        '    End Using
        'Next
        'Watch.Stop()
        'Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())


        Watch.Reset()
        Watch.Start()
        Dim ba As Byte()
        Using m As New MemoryStream
            Dim w As New NetworkFormatter()
            w.Serialize(m, TEST1)
            ba = m.ToArray
        End Using
        Watch.Stop()
        Debug.WriteLine("Custom: " + Watch.ElapsedMilliseconds.ToString())
        Watch.Start()
        Dim cust64 = Convert.ToBase64String(ba)
        Watch.Stop()
        Debug.WriteLine("custom to BASE64: " + Watch.ElapsedMilliseconds.ToString())



        'Watch.Reset()
        'Watch.Start()
        'Dim Serializer = New JsonSerializer()
        'Dim BSONSER As Byte()
        'Using memorystream As New MemoryStream()
        '    Dim bw As New Newtonsoft.Json.Bson.BsonWriter(memorystream)
        '    Serializer.Serialize(bw, TEST1)
        '    BSONSER = memorystream.ToArray()
        'End Using
        'Watch.Stop()
        'Debug.WriteLine("BSON: " + Watch.ElapsedMilliseconds.ToString())
        'Watch.Start()
        'Dim BSONBase64 = Convert.ToBase64String(BSONSER)
        'Watch.Stop()
        'Debug.WriteLine("BSON to BASE64: " + Watch.ElapsedMilliseconds.ToString())
        'Watch.Reset()
        'Watch.Start()
        'Dim BSONDES As TESTSER
        'Using memorystream As New MemoryStream(BSONSER)
        '    Dim bw As New Newtonsoft.Json.Bson.BsonReader(memorystream)
        '    BSONDES = Serializer.Deserialize(bw, GetType(TESTSER))
        'End Using
        'Watch.Stop()
        'Debug.WriteLine("BSON DESER: " + Watch.ElapsedMilliseconds.ToString())


        'Watch.Reset()
        'Watch.Start()
        'Dim SharDoc As Byte()
        'Using memorystream As New MemoryStream()
        '    MessageShark.MessageSharkSerializer.Serialize(TEST1, memorystream)
        '    SharDoc = memorystream.ToArray()
        'End Using
        'Watch.Stop()
        'Debug.WriteLine("Shark: " + Watch.ElapsedMilliseconds.ToString())
        'Watch.Start()
        'Dim Shark64 = Convert.ToBase64String(SharDoc)
        'Watch.Stop()
        'Debug.WriteLine("Shark to BASE64: " + Watch.ElapsedMilliseconds.ToString())
        'Watch.Reset()
        'Watch.Start()
        'Dim SharkDES As TESTSER = MessageShark.MessageSharkSerializer.Deserialize(GetType(TESTSER), SharDoc)
        'Watch.Stop()
        'Debug.WriteLine("Shark DESER: " + Watch.ElapsedMilliseconds.ToString())


        Watch.Reset()
        Watch.Start()
        Dim wiredoc As Byte()
        Dim wires As New Wire.Serializer()
        Using memorystream As New MemoryStream()
            wires.Serialize(TEST1, memorystream)
            wiredoc = memorystream.ToArray()
        End Using
        Watch.Stop()
        Debug.WriteLine("wire: " + Watch.ElapsedMilliseconds.ToString())
        Watch.Start()
        Dim wire64 = Convert.ToBase64String(wiredoc)
        Watch.Stop()
        Debug.WriteLine("wire to BASE64: " + Watch.ElapsedMilliseconds.ToString())
        Watch.Reset()
        Watch.Start()
        Dim wiredes As Object
        Using memorystream As New MemoryStream(wiredoc)
            wiredes = wires.Deserialize(memorystream)
        End Using
        Watch.Stop()
        Debug.WriteLine("wire DESER: " + Watch.ElapsedMilliseconds.ToString())


        Return ""
    End Function

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
End Class


Public Class TESTSER
    Public Property Test1 As String
    Public Property Test2 As Integer
    Public Property Test3 As Decimal
    Public Property Test4 As Boolean
    Public Property Test5 As DateTime
    Public Property Test6 As Object
    Public Property Test7 As List(Of String)
    Private Property Test8 As String = "fffff"
    Public Property Test9 As Integer?

    Sub New()
        Test7 = New List(Of String)
    End Sub
End Class

Public Class TESTSER2
    Public Property Test1 As DateTimeOffset
    Public Property Test2 As Guid
End Class