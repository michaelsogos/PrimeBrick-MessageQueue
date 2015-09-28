Imports System.IO
Imports System.Runtime.Serialization
Imports System.Text

Public Class NetworkFormatter
    Implements IFormatter

    Private _context As StreamingContext

    Public Sub New()
        _context = New StreamingContext(StreamingContextStates.All)
    End Sub

    Public Property Binder As SerializationBinder Implements IFormatter.Binder
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As SerializationBinder)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property Context As StreamingContext Implements IFormatter.Context
        Get
            Return _context
        End Get
        Set(value As StreamingContext)
            _context = value
        End Set
    End Property

    Public Property SurrogateSelector As ISurrogateSelector Implements IFormatter.SurrogateSelector
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As ISurrogateSelector)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Sub Serialize(serializationStream As Stream, graph As Object) Implements IFormatter.Serialize
        Dim SerializedDocument As Byte()
        'Dim SerializedDocumentSize As Byte() = BitConverter.GetBytes(SerializeNestedObject(graph, SerializedDocument))
        Dim SerializedDocumentSize As Byte() = FastConverter.GetBytes(SerializeNestedObject(graph, SerializedDocument))
        serializationStream.Write(SerializedDocumentSize, 0, SerializedDocumentSize.Length)
        serializationStream.Write(SerializedDocument, 0, SerializedDocument.Length) 'PROPERTY NESTED OBJECT
    End Sub

    Public Function Deserialize(serializationStream As Stream) As Object Implements IFormatter.Deserialize
        Dim DocumentSizeBytes = New Byte(3) {}
        serializationStream.Read(DocumentSizeBytes, 0, 4)
        Dim DocumentSize As Integer = BitConverter.ToInt32(DocumentSizeBytes, 0)

        Return Nothing
    End Function

    Private Function ConvertTypeToByte(ByRef ObjectType As Type) As Byte
        Select Case ObjectType
            Case GetType(Double)
                Return &H01
            Case GetType(String)
                Return &H02
            Case GetType(Guid), GetType(Decimal)
                Return &H05
            Case GetType(Boolean)
                Return &H08
            Case GetType(DateTime), GetType(Date), GetType(DateTimeOffset)
                Return &H09
            Case GetType(Integer), GetType(Int32), GetType(UInt32), GetType(UInteger)
                Return &H10
            Case GetType(TimeSpan)
                Return &H11
            Case GetType(Int64), GetType(Long), GetType(ULong), GetType(UInt64)
                Return &H12
            Case GetType(Int16), GetType(Short), GetType(UShort), GetType(UInt16)
                Return &H13
            Case Else
                If Not ObjectType.IsGenericType AndAlso (ObjectType.IsClass OrElse ObjectType.IsNested) Then Return &H03
                If (ObjectType.IsGenericType AndAlso GetType(IEnumerable).IsAssignableFrom(ObjectType)) OrElse ObjectType.IsArray Then Return &H04
                If ObjectType.IsGenericType AndAlso ObjectType.GetGenericTypeDefinition = GetType(Nullable(Of )) Then Return &H0A
        End Select

        Throw New NotImplementedException(String.Format("Cannot serialize a property with type [{0}].", ObjectType.Name))
    End Function

    Private Sub ConvertBinaryTypeToByte(ByRef Stream As IO.Stream, ByRef BinaryValue As Object)
        Select Case BinaryValue.GetType()
            Case GetType(Guid)
                Stream.WriteByte(&H04) 'UUID or GUID //Java work BigEndian byte order while C# use LittleEndian
                Dim ConvertedValue = DirectCast(BinaryValue, Guid).ToByteArray()
                Stream.Write(ConvertedValue, 0, ConvertedValue.Length)
            Case GetType(Decimal)
                Stream.WriteByte(&H06) 'Decimal //A decimal can be serialized as 4 integers in a byte array (its size is always 16 bytes)
                For Each i In Decimal.GetBits(BinaryValue)
                    'Dim BitsinByte = BitConverter.GetBytes(i)
                    Dim BitsinByte = FastConverter.GetBytes(i)
                    Stream.Write(BitsinByte, 0, BitsinByte.Length)
                Next
            Case Else
                Throw New NotImplementedException(String.Format("Cannot serialize a binary value with type [{0}].", BinaryValue.GetType().FullName))
        End Select

    End Sub

    Private Function ConvertNullableTypeToByte(ByRef NullableType As Type) As Byte
        Return ConvertTypeToByte(Nullable.GetUnderlyingType(NullableType))
    End Function

    Private Function SerializeNestedObject(ByRef NestedGraph As Object, ByRef NestedDocument As Byte()) As Integer
        Using Stream As New MemoryStream()
            Dim GraphMemebers = NestedGraph.GetType.GetProperties
            For Each Member In GraphMemebers
                Dim ObjectTypeByte = ConvertTypeToByte(Member.PropertyType)
                Stream.WriteByte(ObjectTypeByte) 'TYPE
                Dim MemberNameBytes = Encoding.UTF8.GetBytes(Member.Name)
                'Dim MemberNameLengthBytes = BitConverter.GetBytes(MemberNameBytes.Length)
                Dim MemberNameLengthBytes = FastConverter.GetBytes(MemberNameBytes.Length)
                Stream.Write(MemberNameLengthBytes, 0, MemberNameLengthBytes.Length)
                Stream.Write(MemberNameBytes, 0, MemberNameBytes.Length) 'PROPERTY NAME

                If ObjectTypeByte = &H03 Then 'NESTED OBJECT
                    Dim SerializedDocument As Byte()
                    'Dim SerializedDocumentSize = BitConverter.GetBytes(SerializeNestedObject(Member.GetValue(NestedGraph), SerializedDocument))
                    Dim SerializedDocumentSize = FastConverter.GetBytes(SerializeNestedObject(Member.GetValue(NestedGraph), SerializedDocument))
                    Stream.Write(SerializedDocumentSize, 0, SerializedDocumentSize.Length)
                    Stream.Write(SerializedDocument, 0, SerializedDocument.Length)
                ElseIf ObjectTypeByte = &H04 Then 'ARRAY
                    Dim ArrayTypes = Member.PropertyType.GetGenericArguments()
                    Dim ArrayTypeByte As Byte
                    If ArrayTypes.Count = 1 Then 'LIST
                        ArrayTypeByte = ConvertTypeToByte(ArrayTypes(0)) 'ARRAY TYPE
                    ElseIf ArrayTypes.Count = 2 Then 'DICTIONARY
                        Stream.WriteByte(&H04)
                        Dim KeyTypeByte = ConvertTypeToByte(ArrayTypes(0))
                        Stream.WriteByte(KeyTypeByte)
                        Dim ValueTypeByte = ConvertTypeToByte(ArrayTypes(1))
                        Stream.WriteByte(ValueTypeByte)
                        Dim SerializedDictionary As Byte()
                        'Dim SerializedDictionarySize = BitConverter.GetBytes(SerializeDictionary(Member.GetValue(NestedGraph), KeyTypeByte, ValueTypeByte, SerializedDictionary))
                        Dim SerializedDictionarySize = FastConverter.GetBytes(SerializeDictionary(Member.GetValue(NestedGraph), KeyTypeByte, ValueTypeByte, SerializedDictionary))
                        Stream.Write(SerializedDictionarySize, 0, SerializedDictionarySize.Length)
                        Stream.Write(SerializedDictionary, 0, SerializedDictionary.Length)
                        Continue For
                    Else
                        ArrayTypeByte = &H00 'ARRAY UNDEFINED TYPE
                    End If
                    Stream.WriteByte(ArrayTypeByte)
                    For Each Value In Member.GetValue(NestedGraph)
                        SerializePrimitiveValues(Stream, ArrayTypeByte, Value)
                    Next
                ElseIf ObjectTypeByte = &H05 Then 'BINARY DATA
                    ConvertBinaryTypeToByte(Stream, Member.GetValue(NestedGraph))
                ElseIf ObjectTypeByte = &H0A Then 'NULLABLE
                    Stream.WriteByte(ConvertNullableTypeToByte(Member.PropertyType))
                    SerializePrimitiveValues(Stream, ObjectTypeByte, Member.GetValue(NestedGraph))
                Else
                    SerializePrimitiveValues(Stream, ObjectTypeByte, Member.GetValue(NestedGraph))
                End If
            Next
            NestedDocument = Stream.ToArray()
        End Using

        Return NestedDocument.Length
    End Function

    Private Sub SerializePrimitiveValues(ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef ObjectValue As Object)
        Select Case ObjectTypeByte
            Case &H02 'STRING
                Dim StringBytes = Encoding.UTF8.GetBytes(ObjectValue)
                'Dim StringBytesLength = BitConverter.GetBytes(StringBytes.length)
                Dim StringBytesLength = FastConverter.GetBytes(StringBytes.length)
                Stream.Write(StringBytesLength, 0, StringBytesLength.Length)
                Stream.Write(StringBytes, 0, StringBytes.length)
            Case &H00 'NULL
                SerializePrimitiveValues(Stream, ConvertTypeToByte(ObjectValue.GetType()), ObjectValue)
            Case &H09 'DATETIME
                'Dim LongBytes = BitConverter.GetBytes(ObjectValue.Ticks)
                Dim LongBytes = FastConverter.GetBytes(ObjectValue.Ticks)
                Stream.Write(LongBytes, 0, LongBytes.length)
            Case &H0A 'NULLABLE
                If ObjectValue Is Nothing Then
                    Stream.WriteByte(&H00)
                Else
                    SerializePrimitiveValues(Stream, ConvertTypeToByte(Nullable.GetUnderlyingType(ObjectValue.GetType())), ObjectValue)
                End If
            Case &H10, &H08, &H12, &H13 'INTEGER, BOOL, LONG, SHORT
                Dim Bytes = FastConverter.GetBytes(ObjectValue)
                Stream.Write(Bytes, 0, Bytes.Length) 'PROPERTY VALUE            
            Case Else
                'Dim Bytes = BitConverter.GetBytes(ObjectValue)
                'Stream.Write(Bytes, 0, Bytes.length) 'PROPERTY VALUE
                Throw New ArgumentOutOfRangeException("ObjectTypeByte", String.Format("The passed byte value [{0}] is out of valid range!", ObjectTypeByte))
        End Select
    End Sub

    Private Function SerializeDictionary(ByRef Dictionary As IDictionary, ByRef KeyTypeByte As Byte, ByRef ValueTypeByte As Byte, ByRef SerializedDictionary As Byte()) As Integer
        Using Stream As New MemoryStream()
            For Each KV As DictionaryEntry In Dictionary
                SerializePrimitiveValues(Stream, KeyTypeByte, KV.Key)
                SerializePrimitiveValues(Stream, ValueTypeByte, KV.Value)
            Next
            SerializedDictionary = Stream.ToArray
        End Using

        Return SerializedDictionary.Length
    End Function
End Class


Public Class FastConverter
    Public Shared Function GetBytes(value As Integer) As Byte()
        Dim Bytes = New Byte(3) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue
        Bytes(2) = (value >> 16) And Byte.MaxValue
        Bytes(3) = (value >> 24) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As UInteger) As Byte()
        Dim Bytes = New Byte(3) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue
        Bytes(2) = (value >> 16) And Byte.MaxValue
        Bytes(3) = (value >> 24) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As Long) As Byte()
        Dim Bytes = New Byte(7) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue
        Bytes(2) = (value >> 16) And Byte.MaxValue
        Bytes(3) = (value >> 24) And Byte.MaxValue
        Bytes(4) = (value >> 32) And Byte.MaxValue
        Bytes(5) = (value >> 40) And Byte.MaxValue
        Bytes(6) = (value >> 48) And Byte.MaxValue
        Bytes(7) = (value >> 56) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As ULong) As Byte()
        Dim Bytes = New Byte(7) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue
        Bytes(2) = (value >> 16) And Byte.MaxValue
        Bytes(3) = (value >> 24) And Byte.MaxValue
        Bytes(4) = (value >> 32) And Byte.MaxValue
        Bytes(5) = (value >> 40) And Byte.MaxValue
        Bytes(6) = (value >> 48) And Byte.MaxValue
        Bytes(7) = (value >> 56) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As Short) As Byte()
        Dim Bytes = New Byte(1) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As UShort) As Byte()
        Dim Bytes = New Byte(1) {}
        Bytes(0) = value And Byte.MaxValue
        Bytes(1) = (value >> 8) And Byte.MaxValue

        Return Bytes
    End Function

    Public Shared Function GetBytes(value As Boolean) As Byte()
        Dim Bytes = New Byte(0) {}
        Bytes(0) = value And &H01
        Return Bytes
    End Function
End Class

