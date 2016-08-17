Imports System.Collections.Specialized
Imports System.Dynamic
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Runtime.Serialization
Imports System.Text

Public Class NetworkSerializer
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

#Region "Serializer"
    Private Function ConvertTypeToByte(ByRef ObjectType As Type) As Byte
        'Reserved non-primitive ObjectType:
        '&H3 = Complex Object
        '&H4 = Array and Dictionary
        '&H6 = Enumerator
        '&HA = Nullable
        Select Case ObjectType
            Case GetType(Object)
                Return &H0
            Case GetType(Double)
                Return &H1
            Case GetType(String)
                Return &H2
            Case GetType(Guid), GetType(Decimal)
                Return &H5
            Case GetType(Single)
                Return &H7
            Case GetType(Boolean)
                Return &H8
            Case GetType(DateTime), GetType(DateTimeOffset), GetType(TimeSpan)
                Return &H9
            Case GetType(Integer), GetType(Int32)
                Return &H10
            Case GetType(UInt32), GetType(UInteger)
                Return &H11
            Case GetType(Int64), GetType(Long)
                Return &H12
            Case GetType(Int16), GetType(Short)
                Return &H13
            Case GetType(ULong), GetType(UInt64)
                Return &H14
            Case GetType(UShort), GetType(UInt16)
                Return &H15
            Case Else 'Not Primitives
                If IsArrayOrDictionaryObject(ObjectType) Then Return &H4 'Array or Dictionary
                If (Not ObjectType.IsGenericType AndAlso (ObjectType.IsClass OrElse ObjectType.IsNested)) OrElse ObjectType.IsConstructedGenericType Then Return &H3 'Complex Object
                If ObjectType.IsGenericType AndAlso ObjectType.GetGenericTypeDefinition = GetType(Nullable(Of )) Then Return &HA 'Nullable
                If ObjectType.IsEnum Then Return &H6 'Enum
        End Select

        Throw New NotImplementedException(String.Format("Cannot serialize a property with type [{0}].", ObjectType.Name))
    End Function

    Public Sub Serialize(serializationStream As Stream, graph As Object) Implements IFormatter.Serialize
        Dim SerializedDocument As Byte()
        Dim SerializedDocumentSize As Byte() = FastConverter.BigEndian.GetBytes(SerializeNestedObject(graph, SerializedDocument))
        serializationStream.Write(SerializedDocumentSize, 0, SerializedDocumentSize.Length)
        serializationStream.Write(SerializedDocument, 0, SerializedDocument.Length) 'PROPERTY NESTED OBJECT
    End Sub

    Private Function SerializeNestedObject(ByRef NestedGraph As Object, ByRef NestedDocument As Byte()) As Integer
        Using Stream As New MemoryStream()
            Dim NestedGraphType = NestedGraph.GetType
            If IsPrimitiveObject(NestedGraphType) Then
                Dim ObjectTypeByte = ConvertTypeToByte(NestedGraphType)
                Stream.WriteByte(ObjectTypeByte) 'TYPE
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                SerializePrimitiveValues(Stream, ObjectTypeByte, NestedGraph)
            ElseIf IsArrayOrDictionaryObject(NestedGraphType) Then
                Stream.WriteByte(&H4) 'Array or Dictionary
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                Stream.WriteByte(0) 'PROPERTY ZERO BYTE - Because with primitive object no property exist
                SerializeArrayOrDictionary(Stream, NestedGraph)
            Else
                If (NestedGraphType.GetProperties.Count <= 0) Then Throw New Exception(String.Format("The object {0} doesn't contains public properties!", NestedGraphType.Name))
                For Each Member In NestedGraphType.GetProperties
                    Dim MemberValue = Member.GetValue(NestedGraph)
                    Dim ObjectTypeByte = ConvertTypeToByte(MemberValue.GetType())
                    Stream.WriteByte(ObjectTypeByte) 'TYPE
                    Dim MemberNameBytes = Encoding.UTF8.GetBytes(Member.Name)
                    'Dim MemberNameLengthBytes = BitConverter.GetBytes(MemberNameBytes.Length)
                    Dim MemberNameLengthBytes = FastConverter.BigEndian.GetBytes(MemberNameBytes.Length)
                    Stream.Write(MemberNameLengthBytes, 0, MemberNameLengthBytes.Length)
                    Stream.Write(MemberNameBytes, 0, MemberNameBytes.Length) 'PROPERTY NAME

                    Select Case ObjectTypeByte
                        Case &H3 'NESTED OBJECT
                            Dim SerializedDocument As Byte()
                            Dim SerializedDocumentSize = FastConverter.BigEndian.GetBytes(SerializeNestedObject(MemberValue, SerializedDocument))
                            Stream.Write(SerializedDocumentSize, 0, SerializedDocumentSize.Length)
                            Stream.Write(SerializedDocument, 0, SerializedDocument.Length)
                        Case &H4 'ARRAY
                            SerializeArrayOrDictionary(Stream, MemberValue)
                        Case &H5 'BINARY DATA
                            SerializeBinaryObject(Stream, MemberValue)
                        Case &H6 'ENUM
                            SerializePrimitiveValues(Stream, &H10, MemberValue)
                        Case &HA 'NULLABLE
                            Stream.WriteByte(ConvertNullableTypeToByte(Member.PropertyType))
                            SerializePrimitiveValues(Stream, ObjectTypeByte, MemberValue)
                        Case Else
                            SerializePrimitiveValues(Stream, ObjectTypeByte, MemberValue)
                    End Select
                Next
            End If
            NestedDocument = Stream.ToArray()
        End Using

        Return NestedDocument.Length
    End Function

    Private Sub SerializePrimitiveValues(ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef ObjectValue As Object)
        Select Case ObjectTypeByte
            Case &H0 'NULL
                SerializePrimitiveValues(Stream, ConvertTypeToByte(ObjectValue.GetType()), ObjectValue)
            Case &H2 'STRING
                Dim StringBytes = Encoding.UTF8.GetBytes(ObjectValue)
                Dim StringBytesLength = FastConverter.BigEndian.GetBytes(StringBytes.length)
                Stream.Write(StringBytesLength, 0, StringBytesLength.Length)
                Stream.Write(StringBytes, 0, StringBytes.length)
            Case &H5 'BINARY DATA
                SerializeBinaryObject(Stream, ObjectValue)
            Case &H9 'DATETIME
                SerializeDateTimeObject(Stream, ObjectValue)
            Case &HA 'NULLABLE
                If ObjectValue Is Nothing Then
                    Stream.WriteByte(&H0)
                Else
                    SerializePrimitiveValues(Stream, ConvertTypeToByte(Nullable.GetUnderlyingType(ObjectValue.GetType())), ObjectValue)
                End If
            Case &H1, &H7, &H8, &H10, &H11, &H12, &H13, &H14, &H15 'DOUBLE, SINGLE, BOOL, INTEGER, UNSIGNED INTEGER, LONG, SHORT, UNSIGNED LONG, UNSIGNED SHORT
                Dim Bytes = FastConverter.BigEndian.GetBytes(ObjectValue)
                Stream.Write(Bytes, 0, Bytes.Length) 'PROPERTY VALUE            
            Case Else
                Throw New ArgumentOutOfRangeException("ObjectTypeByte", String.Format("The passed byte value [{0}] as ObjectType is out of valid range or not implemented yet!", ObjectTypeByte))
        End Select
    End Sub

    Private Sub SerializeArrayOrDictionary(ByRef Stream As IO.Stream, ByRef Value As Object)
        Dim ValueType = Value.GetType()
        Dim ValueTypeByte As Byte
        Dim LengthBytes As Byte()

        If ValueType.IsArray Then 'Array
            ValueTypeByte = ConvertTypeToByte(ValueType.GetElementType())
            LengthBytes = FastConverter.BigEndian.GetBytes(Value.Length)
        ElseIf GetType(IDictionary).IsAssignableFrom(ValueType) OrElse 'Dictionary
        GetType(NameObjectCollectionBase).IsAssignableFrom(ValueType) OrElse 'NameObjectCollectionBase
        GetType(StringDictionary).IsAssignableFrom(ValueType) Then 'StringDictionary 
            ValueTypeByte = &H4
            LengthBytes = FastConverter.BigEndian.GetBytes(Value.Count)
        ElseIf GetType(ICollection).IsAssignableFrom(ValueType) Then 'Collection
            If ValueType.GenericTypeArguments.Length > 0 Then
                ValueTypeByte = ConvertTypeToByte(ValueType.GenericTypeArguments(0))
            Else
                ValueTypeByte = &H0
            End If
            LengthBytes = FastConverter.BigEndian.GetBytes(Value.Count)
        Else
            Throw New NotImplementedException(String.Format("Cannot serialize the type [{0}].", ValueType.FullName))
        End If

        Stream.WriteByte(ValueTypeByte)
        Stream.Write(LengthBytes, 0, LengthBytes.Count)

        Select Case ValueTypeByte
            Case &H4 'DICTIONARY
                If GetType(NameObjectCollectionBase).IsAssignableFrom(ValueType) OrElse
                    GetType(StringDictionary).IsAssignableFrom(ValueType) Then
                    'TODO Michael Sogos: cannot get the value from NameObjectCollectionBase for a given KEY or INDEX
                    'SerializeDictionary(Stream, Value)
                    Throw New NotImplementedException(String.Format("The type [{0}] is not supperted yet!", Value.GetType().Name))
                Else
                    SerializeDictionary(Stream, Value, ValueType.GenericTypeArguments)
                End If
            Case Else 'LIST, ARRAY
                SerializeArray(Stream, Value, ValueTypeByte)
        End Select
    End Sub

    Private Sub SerializeDictionary(ByRef Stream As IO.Stream, ByRef Dictionary As Object, ByRef DictionaryTypes As Type())
        Dim KeyTypeByte As Byte = If(DictionaryTypes.Length > 0, ConvertTypeToByte(DictionaryTypes(0)), &H0)
        Stream.WriteByte(KeyTypeByte)
        Dim ValueTypeByte As Byte = If(DictionaryTypes.Length > 0, ConvertTypeToByte(DictionaryTypes(1)), &H0)
        Stream.WriteByte(ValueTypeByte)

        For Each KV In Dictionary
            Dim KeyObjectTypeByte As Byte = KeyTypeByte
            If KeyTypeByte = &H0 Then
                KeyObjectTypeByte = ConvertTypeToByte(KV.Key.GetType())
                Stream.WriteByte(KeyObjectTypeByte)
            End If
            SerializePrimitiveValues(Stream, KeyObjectTypeByte, KV.Key)

            SerializeEnumerableValue(Stream, ValueTypeByte, KV.Value)
        Next
    End Sub

    'TODO Michael Sogos: cannot get the value from NameObjectCollectionBase for a given KEY or INDEX
    'Private Sub SerializeDictionary(ByRef Stream As IO.Stream, ByRef Dictionary As NameObjectCollectionBase)
    '    Dim KeyTypeByte As Byte = ConvertTypeToByte(Dictionary.Keys.Get(0).GetType())
    '    Stream.WriteByte(KeyTypeByte)
    '    Dim ValueTypeByte As Byte = ConvertTypeToByte(Dictionary(Dictionary.Keys.Get(0)).GetType())
    '    Stream.WriteByte(ValueTypeByte)

    '    For Each Key In Dictionary
    '        SerializePrimitiveValues(Stream, KeyTypeByte, Key)
    '        SerializePrimitiveValues(Stream, ValueTypeByte, Dictionary(Key))
    '    Next
    'End Sub

    Private Sub SerializeArray(ByRef Stream As IO.Stream, ByRef Array As Object, ByRef ObjectTypeByte As Byte)
        For Each Value In Array
            SerializeEnumerableValue(Stream, ObjectTypeByte, Value)
        Next
    End Sub

    Private Sub SerializeEnumerableValue(ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef Value As Object)
        Select Case ObjectTypeByte
            Case &H3
                Serialize(Stream, Value)
            Case Else
                If ObjectTypeByte = &H0 Then
                    Dim ValueTypeByte = ConvertTypeToByte(Value.GetType())
                    Stream.WriteByte(ValueTypeByte)
                    SerializePrimitiveValues(Stream, ValueTypeByte, Value)
                Else
                    SerializePrimitiveValues(Stream, ObjectTypeByte, Value)
                End If
        End Select
    End Sub

    Private Sub SerializeBinaryObject(ByRef Stream As IO.Stream, ByRef BinaryValue As Object)
        Select Case BinaryValue.GetType()
            Case GetType(Guid)
                Stream.WriteByte(&H4) 'UUID or GUID //Java work BigEndian byte order while C# use LittleEndian
                Dim ConvertedValue = DirectCast(BinaryValue, Guid).ToByteArray()
                Stream.Write(ConvertedValue, 0, ConvertedValue.Length)
            Case GetType(Decimal)
                Stream.WriteByte(&H6) 'Decimal //A decimal can be serialized as 4 integers in a byte array (its size is always 16 bytes)
                For Each i In Decimal.GetBits(BinaryValue)
                    Dim BitsinByte = FastConverter.BigEndian.GetBytes(i)
                    Stream.Write(BitsinByte, 0, BitsinByte.Length)
                Next
            Case Else
                Throw New NotImplementedException(String.Format("Cannot serialize a binary value with type [{0}].", BinaryValue.GetType().FullName))
        End Select
    End Sub

    Private Sub SerializeDateTimeObject(ByRef Stream As IO.Stream, ByRef DateTimeValue As Object)
        Select Case DateTimeValue.GetType()
            Case GetType(TimeSpan)
                Stream.WriteByte(&H1)
                Dim LongBytes = FastConverter.BigEndian.GetBytes(DateTimeValue.Ticks)
                Stream.Write(LongBytes, 0, LongBytes.Length)
            Case GetType(DateTimeOffset)
                Stream.WriteByte(&H2)
                Dim LongBytes = FastConverter.BigEndian.GetBytes(DateTimeValue.Ticks)
                Stream.Write(LongBytes, 0, LongBytes.Length)
                LongBytes = FastConverter.BigEndian.GetBytes(DateTimeValue.Offset.Ticks)
                Stream.Write(LongBytes, 0, LongBytes.Length)
            Case GetType(DateTime)
                Stream.WriteByte(&H3)
                Dim LongBytes = FastConverter.BigEndian.GetBytes(DateTimeValue.Ticks)
                Stream.Write(LongBytes, 0, LongBytes.Length)
            Case Else
                Throw New NotImplementedException(String.Format("Cannot serialize a date or time value with type [{0}].", DateTimeValue.GetType().FullName))
        End Select
    End Sub

    Private Function ConvertNullableTypeToByte(ByRef NullableType As Type) As Byte
        Return ConvertTypeToByte(Nullable.GetUnderlyingType(NullableType))
    End Function
#End Region

#Region "Deserializer with ExpandoObject"
    Private Function ConvertByteToType(ByRef ObjectTypeByte As Byte) As Type
        'Reserved non-primitive ObjectType:
        '&H3 = Complex Object
        '&H4 = Array and Dictionary
        '&H5 = Binary Object (GUID, Decimal)
        '&H6 = Enumerator
        '&H9 = DateTime, Timespan, DeteTimeOffset
        '&HA = Nullable
        Select Case ObjectTypeByte
            Case &H0, &H3
                Return GetType(Object)
            Case &H1
                Return GetType(Double)
            Case &H2
                Return GetType(String)
            Case &H7
                Return GetType(Single)
            Case &H8
                Return GetType(Boolean)
            Case &H10
                Return GetType(Integer)
            Case &H11
                Return GetType(UInteger)
            Case &H12
                Return GetType(Long)
            Case &H13
                Return GetType(Short)
            Case &H14
                Return GetType(ULong)
            Case &H15
                Return GetType(UShort)
        End Select

        Throw New NotImplementedException(String.Format("Cannot convert in a strong TYPE the BYTE [{0}].", ObjectTypeByte))
    End Function

    Public Function Deserialize(serializationStream As Stream) As Object Implements IFormatter.Deserialize
        Return DeserializeObject(Nothing, serializationStream)
    End Function

    Private Function DeserializeNestedObject(ByRef Stream As IO.Stream, ByVal ObjectSize As Integer, ByRef BytesRead As Integer) As Object
        Dim Result As Object = New ExpandoObject
        Dim ResultDict As IDictionary(Of String, Object) = Result
        While Not ((ObjectSize = BytesRead) OrElse (Stream.Length = Stream.Position))   'TODO: Se usiamo il -1 restituito da "Dim ObjectTypeByte = Stream.ReadByte" possiamo forzare l'uscita del loop while, in questo modo tutti gli STREAM che non supportano il POSITION potrebbero funzionare
            Dim ObjectTypeByte = Stream.ReadByte
            If ObjectTypeByte < 0 Then Exit While
            BytesRead += 1

            Dim PropertyNameSizeBytes = New Byte(3) {}
            Stream.Read(PropertyNameSizeBytes, 0, 4)
            BytesRead += 4
            Dim PropertyNameSize As Integer = FastConverter.BigEndian.GetInteger(PropertyNameSizeBytes)

            If (PropertyNameSize <= 0) Then 'Object without property
                Dim ValueBytesRead As Integer = 0
                Dim ScalarResult As Object
                If ObjectTypeByte = &H4 Then
                    ScalarResult = DeserializeArrayOrDictionary(Stream, ValueBytesRead)
                Else
                    ScalarResult = DeserializePrimitiveValues(Stream, ObjectTypeByte, ValueBytesRead)
                End If
                BytesRead += ValueBytesRead
                Return ScalarResult
            Else
                Dim PropertyNameBytes As Byte() = New Byte(PropertyNameSize - 1) {}
                Stream.Read(PropertyNameBytes, 0, PropertyNameSize)
                BytesRead += PropertyNameSize
                Dim PropertyName As String = Encoding.UTF8.GetString(PropertyNameBytes)

                If ObjectTypeByte = &H3 Then 'NESTED OBJECT
                    Dim DocumentSizeBytes = New Byte(3) {}
                    Stream.Read(DocumentSizeBytes, 0, 4)
                    BytesRead += 4
                    Dim DocumentSize As Integer = FastConverter.BigEndian.GetInteger(DocumentSizeBytes)
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializeNestedObject(Stream, DocumentSize, ValueBytesRead)
                    If (DocumentSize <> ValueBytesRead) Then Throw New Exception(String.Format("For the property name [{0}] the object size expected is {1}, but has been deserialized {2} bytes!", PropertyName, DocumentSize, ValueBytesRead))
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H4 Then 'ARRAY
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializeArrayOrDictionary(Stream, ValueBytesRead)
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H5 Then 'BINARY DATA
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializeBinaryObject(Stream, ValueBytesRead)
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H6 Then 'ENUM
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializePrimitiveValues(Stream, &H10, ValueBytesRead)
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &HA Then 'NULLABLE
                    Dim ValueTypeByte = Stream.ReadByte
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializePrimitiveValues(Stream, ValueTypeByte, ValueBytesRead)
                    BytesRead += ValueBytesRead
                Else
                    Dim ValueBytesRead As Integer = 0
                    ResultDict(PropertyName) = DeserializePrimitiveValues(Stream, ObjectTypeByte, ValueBytesRead)
                    BytesRead += ValueBytesRead
                End If
            End If
        End While
        Return Result
    End Function

    Private Function DeserializePrimitiveValues(ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef BytesRead As Integer) As Object
        Select Case ObjectTypeByte
            Case &H0 'NULL
                Return Nothing
            Case &H1 'DOUBLE
                Dim Bytes As Byte() = New Byte(7) {}
                Stream.Read(Bytes, 0, 8)
                BytesRead += 8
                Return FastConverter.BigEndian.GetDouble(Bytes)
            Case &H2 'STRING
                Dim StringSizeBytes = New Byte(3) {}
                Stream.Read(StringSizeBytes, 0, 4)
                BytesRead += 4
                Dim StringSize As Integer = FastConverter.BigEndian.GetInteger(StringSizeBytes)
                Dim StringBytes As Byte() = New Byte(StringSize - 1) {}
                Stream.Read(StringBytes, 0, StringSize)
                BytesRead += StringSize
                Return Encoding.UTF8.GetString(StringBytes)
            Case &H5 'BINARY DATA
                Dim ValueBytesRead As Integer = 0
                Dim BinaryData = DeserializeBinaryObject(Stream, ValueBytesRead)
                BytesRead += ValueBytesRead
                Return BinaryData
            Case &H7 'SINGLE
                Dim Bytes As Byte() = New Byte(3) {}
                Stream.Read(Bytes, 0, 4)
                BytesRead += 4
                Return FastConverter.BigEndian.GetSingle(Bytes)
            Case &H8 'BOOL
                Dim Bytes As Byte() = New Byte(0) {}
                Stream.Read(Bytes, 0, 1)
                BytesRead += 1
                Return FastConverter.BigEndian.GetBoolean(Bytes)
            Case &H9 'DATETIME, DATETIMEOFFSET, TIMESPAN 
                Dim ValueBytesRead As Integer = 0
                Dim BinaryData = DeserializeDateTimeObject(Stream, ValueBytesRead)
                BytesRead += ValueBytesRead
                Return BinaryData
            Case &H10 'INTEGER
                Dim Bytes As Byte() = New Byte(3) {}
                Stream.Read(Bytes, 0, 4)
                BytesRead += 4
                Return FastConverter.BigEndian.GetInteger(Bytes)
            Case &H11 'UNSIGNED INTEGER
                Dim Bytes As Byte() = New Byte(3) {}
                Stream.Read(Bytes, 0, 4)
                BytesRead += 4
                Return FastConverter.BigEndian.GetUInteger(Bytes)
            Case &H12 'LONG
                Dim Bytes As Byte() = New Byte(7) {}
                Stream.Read(Bytes, 0, 8)
                BytesRead += 8
                Return FastConverter.BigEndian.GetLong(Bytes)
            Case &H13 'SHORT
                Dim Bytes As Byte() = New Byte(1) {}
                Stream.Read(Bytes, 0, 2)
                BytesRead += 2
                Return FastConverter.BigEndian.GetShort(Bytes)
            Case &H14 'UNSIGNED LONG
                Dim Bytes As Byte() = New Byte(7) {}
                Stream.Read(Bytes, 0, 8)
                BytesRead += 8
                Return FastConverter.BigEndian.GetULong(Bytes)
            Case &H15 'UNSIGNED SHORT
                Dim Bytes As Byte() = New Byte(1) {}
                Stream.Read(Bytes, 0, 2)
                BytesRead += 2
                Return FastConverter.BigEndian.GetUShort(Bytes)
            Case Else
                Throw New ArgumentOutOfRangeException("ObjectTypeByte", String.Format("The passed byte value [{0}] is out of valid range!", ObjectTypeByte))
        End Select
    End Function

    Private Function DeserializeArrayOrDictionary(ByRef Stream As IO.Stream, ByRef BytesRead As Integer) As Object
        Dim ObjectTypeByte = Stream.ReadByte()
        BytesRead += 1
        Dim ObjectLengthBytes As Byte() = New Byte(3) {}
        Stream.Read(ObjectLengthBytes, 0, 4)
        BytesRead += 4
        Dim ObjectLength = FastConverter.BigEndian.GetInteger(ObjectLengthBytes)

        Select Case ObjectTypeByte
            Case &H4 ' DICTIONARY
                Return DeserializeDictionaryObject(Stream, ObjectLength, BytesRead)
            Case Else
                Return DeserializeArrayObject(Stream, ObjectTypeByte, ObjectLength, BytesRead)
        End Select

    End Function

    Private Function DeserializeArrayObject(ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef ObjectLength As Integer, ByRef BytesRead As Integer)
        Return DeserializeArrayObject(ConvertByteToType(ObjectTypeByte).MakeArrayType(), Stream, ObjectTypeByte, ObjectLength, BytesRead)
    End Function

    Private Function DeserializeDictionaryObject(ByRef Stream As IO.Stream, ByRef ObjectLength As Integer, ByRef BytesRead As Integer)
        Return DeserializeDictionaryObject(Nothing, Stream, ObjectLength, BytesRead)
    End Function

    Private Function DeserializeBinaryObject(ByRef Stream As IO.Stream, ByRef BytesRead As Integer)
        Dim ObjectTypeByte = Stream.ReadByte()
        BytesRead += 1
        Select Case ObjectTypeByte
            Case &H4 'GUID
                Dim BinaryBytes As Byte() = New Byte(15) {} 'GUID è un bytearray di 16 bytes
                Stream.Read(BinaryBytes, 0, 16) 'GUID è un bytearray di 16 bytes
                BytesRead += 16
                Return New Guid(BinaryBytes)
            Case &H6 'DECIMAL
                Dim Bits(3) As Integer
                For x = 0 To 3
                    Dim BinaryBytes As Byte() = New Byte(3) {} 'Decimal è un bytearray di 4 gruppi di 4 bytes (totale 16)
                    Stream.Read(BinaryBytes, 0, 4) 'Decimal è un bytearray di 4 gruppi di 4 bytes (totale 16)
                    BytesRead += 4
                    Bits(x) = FastConverter.BigEndian.GetInteger(BinaryBytes)
                Next
                Return New Decimal(Bits)
            Case Else
                Throw New NotImplementedException(String.Format("Cannot deserialize a binary value of type [{0}].", ObjectTypeByte))
        End Select
    End Function

    Private Function DeserializeDateTimeObject(ByRef Stream As IO.Stream, ByRef BytesRead As Integer)
        Dim ObjectTypeByte = Stream.ReadByte()
        BytesRead += 1
        Select Case ObjectTypeByte
            Case &H1 'TIMESPAN
                Dim Bytes As Byte() = New Byte(7) {} 'TIMESPAN is a bytearray of 8 Bytes
                Stream.Read(Bytes, 0, 8) 'TIMESPAN is a bytearray of 8 Bytes
                BytesRead += 8
                Return New TimeSpan(FastConverter.BigEndian.GetLong(Bytes))
            Case &H2 'DATETIMEOFFSET
                'DATETIMEOFFSET is a bytearray composed by 2 groups of 8 Bytes (DateTime + Offset TimeSpan)
                Dim Bytes As Byte() = New Byte(7) {} 'DateTime Ticks
                Stream.Read(Bytes, 0, 8) 'DateTime Ticks
                BytesRead += 8
                Dim DateTimeTicks = FastConverter.BigEndian.GetLong(Bytes)
                Bytes = New Byte(7) {} 'Offset Timespan Ticks
                Stream.Read(Bytes, 0, 8) 'Offset Timespan Ticks
                BytesRead += 8
                Dim Offset = New TimeSpan(FastConverter.BigEndian.GetLong(Bytes))
                Return New DateTimeOffset(DateTimeTicks, Offset)
            Case &H3 'DATETIME
                Dim Bytes As Byte() = New Byte(7) {} 'DATETIME is a bytearray of 8 Bytes
                Stream.Read(Bytes, 0, 8) 'DATETIME is a bytearray of 8 Bytes
                BytesRead += 8
                Return New DateTime(FastConverter.BigEndian.GetLong(Bytes))
            Case Else
                Throw New NotImplementedException(String.Format("Cannot deserialize a binary value of type [{0}].", ObjectTypeByte))
        End Select
    End Function
#End Region

#Region "Deserializer with StaticType"
    Public Function Deserialize(Of T)(SerializationStream As Stream) As T
        'Dim DocumentSizeBytes = New Byte(3) {}
        'SerializationStream.Read(DocumentSizeBytes, 0, 4)
        'Dim DocumentSize As Integer = FastConverter.BigEndian.GetInteger(DocumentSizeBytes)
        'If DocumentSize + 4 <> SerializationStream.Length Then Throw New Exception(String.Format("The message received have a size of {0} bytes but the system expect to receive {1} bytes!", SerializationStream.Length, DocumentSize + 4))
        'Dim ValueBytesRead As Integer = 0

        'Dim Result = DeserializeNestedObject(GetType(T), SerializationStream, DocumentSize, ValueBytesRead)
        'If (DocumentSize <> ValueBytesRead) Then Throw New Exception(String.Format("The entire object size should be {0}, but has been deserialized {1} bytes!", DocumentSize, ValueBytesRead))

        'Return Result
        Return DeserializeObject(GetType(T), SerializationStream)
    End Function

    Public Function DeserializeObject(ByVal ObjectType As Type, serializationStream As Stream, Optional ByRef BytesRead As Integer = 0)
        Dim DocumentSizeBytes = New Byte(3) {}
        serializationStream.Read(DocumentSizeBytes, 0, 4)
        BytesRead += 4
        Dim DocumentSize As Integer = FastConverter.BigEndian.GetInteger(DocumentSizeBytes)
        Dim ValueBytesRead As Integer = 0

        Dim Result = If(ObjectType Is Nothing,
                            DeserializeNestedObject(serializationStream, DocumentSize, ValueBytesRead),
                            DeserializeNestedObject(ObjectType, serializationStream, DocumentSize, ValueBytesRead))

        If (DocumentSize <> ValueBytesRead) Then Throw New Exception(String.Format("The entire object size should be {0}, but has been deserialized {1} bytes!", DocumentSize, ValueBytesRead))
        BytesRead += ValueBytesRead

        Return Result
    End Function

    Private Function DeserializeNestedObject(ByVal ObjectType As Type, ByRef Stream As IO.Stream, ByVal ObjectSize As Integer, ByRef BytesRead As Integer)
        Dim Result = Nothing
        If ObjectType.GetConstructor(Type.EmptyTypes) IsNot Nothing Then Result = Activator.CreateInstance(ObjectType)
        While Not ((ObjectSize = BytesRead) OrElse (Stream.Length = Stream.Position))  'TODO: Se usiamo il -1 restituito da "Dim ObjectTypeByte = Stream.ReadByte" possiamo forzare l'uscita del loop while, in questo modo tutti gli STREAM che non supportano il POSITION potrebbero funzionare
            Dim ObjectTypeByte = Stream.ReadByte
            If ObjectTypeByte < 0 Then Exit While
            BytesRead += 1

            Dim PropertyNameSizeBytes = New Byte(3) {}
            Stream.Read(PropertyNameSizeBytes, 0, 4)
            BytesRead += 4
            Dim PropertyNameSize As Integer = FastConverter.BigEndian.GetInteger(PropertyNameSizeBytes)

            If (PropertyNameSize <= 0) Then 'Object without property
                Dim ValueBytesRead As Integer = 0
                Dim ScalarResult As Object
                If ObjectTypeByte = &H4 Then
                    ScalarResult = DeserializeArrayOrDictionary(ObjectType, Stream, ValueBytesRead)
                Else
                    ScalarResult = DeserializePrimitiveValues(Stream, ObjectTypeByte, ValueBytesRead)
                End If
                BytesRead += ValueBytesRead
                Return ScalarResult
            Else
                Dim PropertyNameBytes As Byte() = New Byte(PropertyNameSize - 1) {}
                Stream.Read(PropertyNameBytes, 0, PropertyNameSize)
                BytesRead += PropertyNameSize
                Dim PropertyName As String = Encoding.UTF8.GetString(PropertyNameBytes)

                If ObjectTypeByte = &H3 Then 'NESTED OBJECT
                    Dim DocumentSizeBytes = New Byte(3) {}
                    Stream.Read(DocumentSizeBytes, 0, 4)
                    BytesRead += 4
                    Dim DocumentSize As Integer = FastConverter.BigEndian.GetInteger(DocumentSizeBytes)
                    Dim ValueBytesRead As Integer = 0

                    Dim NestedObjectType = Result.GetType().GetProperty(PropertyName).PropertyType
                    If TypeOf NestedObjectType Is Object Then
                        Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializeNestedObject(Stream, DocumentSize, ValueBytesRead))
                    Else
                        Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializeNestedObject(NestedObjectType, Stream, DocumentSize, ValueBytesRead))
                    End If
                    If (DocumentSize <> ValueBytesRead) Then Throw New Exception(String.Format("For the property name [{0}] the object size expected is {1}, but has been deserialized {2} bytes!", PropertyName, DocumentSize, ValueBytesRead))
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H4 Then 'ARRAY
                    Dim ValueBytesRead As Integer = 0
                    Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializeArrayOrDictionary(Stream, ValueBytesRead))
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H5 Then 'BINARY DATA
                    Dim ValueBytesRead As Integer = 0
                    Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializeBinaryObject(Stream, ValueBytesRead))
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &H6 Then 'ENUM
                    Dim ValueBytesRead As Integer = 0
                    Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializePrimitiveValues(Stream, &H10, ValueBytesRead))
                    BytesRead += ValueBytesRead
                ElseIf ObjectTypeByte = &HA Then 'NULLABLE
                    Dim ValueBytesRead As Integer = 0
                    Dim ValueTypeByte = Stream.ReadByte
                    Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializePrimitiveValues(Stream, ValueTypeByte, ValueBytesRead))
                    BytesRead += ValueBytesRead
                Else
                    Dim ValueBytesRead As Integer = 0
                    Result.GetType().GetProperty(PropertyName).SetValue(Result, DeserializePrimitiveValues(Stream, ObjectTypeByte, ValueBytesRead))
                    BytesRead += ValueBytesRead
                End If
            End If
        End While
        Return Result
    End Function

    Private Function DeserializeArrayOrDictionary(ByVal ObjectType As Type, ByRef Stream As IO.Stream, ByRef BytesRead As Integer)
        Dim ObjectTypeByte = Stream.ReadByte()
        BytesRead += 1
        Dim ObjectLengthBytes As Byte() = New Byte(3) {}
        Stream.Read(ObjectLengthBytes, 0, 4)
        BytesRead += 4
        Dim ObjectLength = FastConverter.BigEndian.GetInteger(ObjectLengthBytes)

        Select Case ObjectTypeByte
            Case &H4 ' DICTIONARY
                Return DeserializeDictionaryObject(ObjectType, Stream, ObjectLength, BytesRead)
            Case Else
                Return DeserializeArrayObject(ObjectType, Stream, ObjectTypeByte, ObjectLength, BytesRead)
        End Select
    End Function

    Private Function DeserializeArrayObject(ByRef ObjectType As Type, ByRef Stream As IO.Stream, ByRef ObjectTypeByte As Byte, ByRef ObjectLength As Integer, ByRef BytesRead As Integer)
        If ObjectType.IsArray Then
            Dim Result = Array.CreateInstance(ObjectType.GetElementType(), ObjectLength)
            Dim ValueTypeByte As Byte = ObjectTypeByte
            For x = 0 To ObjectLength - 1
                Select Case ObjectTypeByte
                    Case &H3
                        If (ObjectType.GetElementType() = GetType(Object)) Then
                            Result(x) = DeserializeObject(Nothing, Stream, BytesRead)
                        Else
                            Result(x) = DeserializeObject(ObjectType.GetElementType(), Stream, BytesRead)
                        End If
                    Case Else
                        If ObjectTypeByte = &H0 Then
                            ValueTypeByte = Stream.ReadByte()
                            BytesRead += 1
                        End If
                        Result(x) = DeserializePrimitiveValues(Stream, ValueTypeByte, BytesRead)
                End Select
            Next
            Return Result
        ElseIf GetType(IList).IsAssignableFrom(ObjectType) Then
            Dim Result = Activator.CreateInstance(ObjectType)
            Dim ValueTypeByte As Byte = ObjectTypeByte
            For x = 1 To ObjectLength
                Select Case ObjectTypeByte
                    Case &H3
                        If (ObjectType.GenericTypeArguments()(0) = GetType(Object)) Then
                            Result.add(DeserializeObject(Nothing, Stream, BytesRead))
                        Else
                            Result.add(DeserializeObject(ObjectType.GenericTypeArguments()(0), Stream, BytesRead))
                        End If
                    Case Else
                        If ObjectTypeByte = &H0 Then
                            ValueTypeByte = Stream.ReadByte()
                            BytesRead += 1
                        End If
                        Result.Add(DeserializePrimitiveValues(Stream, ValueTypeByte, BytesRead))
                End Select
            Next
            Return Result
        ElseIf GetType(ICollection).IsAssignableFrom(ObjectType) Then
            Dim InitializerArray(ObjectLength - 1) As Object
            Dim ValueTypeByte As Byte = ObjectTypeByte
            Dim UniqueTypeByte As Byte = &H0
            Dim IsTypeUnique = True

            For x = 1 To ObjectLength
                Select Case ObjectTypeByte
                    Case &H3
                        If (ObjectType.GenericTypeArguments()(0) = GetType(Object)) Then
                            InitializerArray(x - 1) = DeserializeObject(Nothing, Stream, BytesRead)
                        Else
                            InitializerArray(x - 1) = DeserializeObject(ObjectType.GenericTypeArguments()(0), Stream, BytesRead)
                        End If
                    Case Else
                        If ObjectTypeByte = &H0 Then
                            ValueTypeByte = Stream.ReadByte()
                            BytesRead += 1
                        End If
                        InitializerArray(x - 1) = DeserializePrimitiveValues(Stream, ValueTypeByte, BytesRead)
                End Select

                If UniqueTypeByte <> &H0 AndAlso UniqueTypeByte <> ValueTypeByte Then IsTypeUnique = False
                If UniqueTypeByte = &H0 Then UniqueTypeByte = ValueTypeByte
            Next

            If IsTypeUnique Then
                Dim InizializerParameter As Array
                If UniqueTypeByte = &H0 OrElse UniqueTypeByte = &H3 Then
                    InizializerParameter = Array.CreateInstance(ObjectType.GenericTypeArguments()(0), ObjectLength)
                Else
                    InizializerParameter = Array.CreateInstance(ConvertByteToType(UniqueTypeByte), ObjectLength)
                End If
                InitializerArray.CopyTo(InizializerParameter, 0)
                Return Activator.CreateInstance(ObjectType, InizializerParameter)
            Else
                Return Activator.CreateInstance(ObjectType, InitializerArray)
            End If
        Else
            Throw New NotImplementedException(String.Format("Cannot deserialize an array object of type [{0}].", ObjectType.Name))
        End If
    End Function

    Private Function DeserializeDictionaryObject(ByRef ObjectType As Type, ByRef Stream As IO.Stream, ByRef ObjectLength As Integer, ByRef BytesRead As Integer)
        Dim KeyTypeByte = Stream.ReadByte()
        BytesRead += 1
        Dim KeyType = ConvertByteToType(KeyTypeByte)
        Dim ValueTypeByte = Stream.ReadByte()
        BytesRead += 1
        Dim ValueType = ConvertByteToType(ValueTypeByte)

        If ObjectType Is Nothing Then
            ObjectType = GetType(Dictionary(Of ,)).MakeGenericType(KeyType, ValueType)
        End If
        Dim Result As IDictionary = Activator.CreateInstance(ObjectType)

        For x = 1 To ObjectLength
            Dim KeyObjectTypeByte As Byte = KeyTypeByte
            If KeyTypeByte = &H0 Then
                KeyObjectTypeByte = Stream.ReadByte()
                BytesRead += 1
            End If
            Dim Key = DeserializePrimitiveValues(Stream, KeyObjectTypeByte, BytesRead)

            Select Case ValueTypeByte
                Case &H3
                    If (ObjectType.GenericTypeArguments()(1) = GetType(Object)) Then
                        Result.Add(Key, DeserializeObject(Nothing, Stream, BytesRead))
                    Else
                        Result.Add(Key, DeserializeObject(ObjectType.GenericTypeArguments()(1), Stream, BytesRead))
                    End If
                Case Else
                    Dim ValueObjectTypeByte As Byte = ValueTypeByte
                    If ValueTypeByte = &H0 Then
                        ValueObjectTypeByte = Stream.ReadByte()
                        BytesRead += 1
                    End If
                    Result.Add(Key, DeserializePrimitiveValues(Stream, ValueObjectTypeByte, BytesRead))
            End Select

        Next

        Return Result
    End Function

#End Region

#Region "Private Helpers"
    Private Function IsPrimitiveObject(ByRef ValueType As Type)
        Return GetType(IComparable).IsAssignableFrom(ValueType) OrElse ValueType.IsPrimitive OrElse ValueType.IsValueType
    End Function

    Private Function IsArrayOrDictionaryObject(ByRef ValueType As Type)
        Return GetType(IEnumerable).IsAssignableFrom(ValueType) OrElse GetType(IEnumerable(Of )).IsAssignableFrom(ValueType) OrElse ValueType.IsArray
    End Function

    Private Function CreateInstanceOfObject(ByRef ObjectType As Type) As Object
        Dim Constructor As ConstructorInfo = ObjectType.TypeInitializer
        If Constructor Is Nothing Then Constructor = ObjectType.GetConstructors().FirstOrDefault()
        If Constructor Is Nothing Then Return Activator.CreateInstance(ObjectType)

        Dim ConstructorParameters As ParameterInfo() = Constructor.GetParameters()
        If ConstructorParameters.Length = 0 Then Return Activator.CreateInstance(ObjectType)

        Dim Parameters(ConstructorParameters.Length - 1) As Object
        For x = 0 To Parameters.Length - 1
            Parameters(x) = Activator.CreateInstance(ConstructorParameters(x).ParameterType)
        Next
        Return Activator.CreateInstance(ObjectType, Parameters)
    End Function
#End Region

End Class

Public Class DeserializedObject
    Inherits DynamicObject

    Public Overrides Function TrySetMember(binder As SetMemberBinder, value As Object) As Boolean
        Return MyBase.TrySetMember(binder, value)
    End Function

End Class

Public Class ObjectBuilder

    Public Property myType As Object
    Public Property myObject As Object
    Public Property myT As TypeBuilder

    Public Sub New()
        'myType = CompileResultType()
        'myObject = Activator.CreateInstance(myType)
    End Sub

    Public Shared Function CompileResultType(tb As TypeBuilder) As Type
        'Dim constructor As ConstructorBuilder = tb.DefineDefaultConstructor(MethodAttributes.[Public] Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName)
        Dim objectType As Type = tb.CreateType()
        Return objectType
    End Function

    Public Shared Function GetModelBuilder() As ModuleBuilder
        Dim typeSignature = "MyDynamicType"
        Dim an = New AssemblyName(typeSignature)
        Dim assemblyBuilder As AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run)
        Dim moduleBuilder As ModuleBuilder = assemblyBuilder.DefineDynamicModule("MyMainModule")
        Return moduleBuilder
    End Function

    Public Shared Function GetTypeBuilder(mb As ModuleBuilder) As TypeBuilder
        Dim tb As TypeBuilder = mb.DefineType("MyDynamicType", TypeAttributes.[Public] Or TypeAttributes.[Class] Or TypeAttributes.AutoClass Or TypeAttributes.AnsiClass Or TypeAttributes.BeforeFieldInit Or TypeAttributes.AutoLayout, Nothing)

        Return tb
    End Function

    Public Shared Sub Finalize(c As ConstructorBuilder)
        Dim cIl = c.GetILGenerator
        cIl.Emit(OpCodes.Ret)
    End Sub

    Public Shared Function CreateConstructor(tb As TypeBuilder) As ConstructorBuilder
        'Return tb.DefineDefaultConstructor(MethodAttributes.[Public] Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName)
        Dim c = tb.DefineConstructor(MethodAttributes.[Public] Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName, CallingConventions.Standard Or CallingConventions.HasThis, New Type() {})
        Dim objType As Type = Type.GetType("System.Object")
        Dim objCtor As ConstructorInfo = objType.GetConstructor(Type.EmptyTypes)
        Dim cIl = c.GetILGenerator
        cIl.Emit(OpCodes.Nop)
        cIl.Emit(OpCodes.Ldarg_0)
        cIl.Emit(OpCodes.Call, objCtor)
        cIl.Emit(OpCodes.Nop)
        Return c
    End Function

    Public Shared Sub CreateProperty(c As ConstructorBuilder, tb As TypeBuilder, propertyName As String, propertyType As Type, value As Object)
        Dim fieldBuilder As FieldBuilder = tb.DefineField("_" & propertyName, propertyType, FieldAttributes.[Private])

        Dim propertyBuilder As PropertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, Nothing)
        Dim getPropMthdBldr As MethodBuilder = tb.DefineMethod("get_" & propertyName, MethodAttributes.[Public] Or MethodAttributes.SpecialName Or MethodAttributes.HideBySig, propertyType, Type.EmptyTypes)
        Dim getIl As ILGenerator = getPropMthdBldr.GetILGenerator()

        getIl.Emit(OpCodes.Ldarg_0)
        getIl.Emit(OpCodes.Ldfld, fieldBuilder)
        getIl.Emit(OpCodes.Ret)

        Dim setPropMthdBldr As MethodBuilder = tb.DefineMethod("set_" & propertyName, MethodAttributes.[Public] Or MethodAttributes.SpecialName Or MethodAttributes.HideBySig, Nothing, {propertyType})

        Dim setIl As ILGenerator = setPropMthdBldr.GetILGenerator()
        Dim modifyProperty As Label = setIl.DefineLabel()
        Dim exitSet As Label = setIl.DefineLabel()

        setIl.MarkLabel(modifyProperty)
        setIl.Emit(OpCodes.Ldarg_0)
        setIl.Emit(OpCodes.Ldarg_1)
        setIl.Emit(OpCodes.Stfld, fieldBuilder)

        setIl.Emit(OpCodes.Nop)
        setIl.MarkLabel(exitSet)
        setIl.Emit(OpCodes.Ret)

        propertyBuilder.SetGetMethod(getPropMthdBldr)
        propertyBuilder.SetSetMethod(setPropMthdBldr)


        Dim cIl = c.GetILGenerator

        cIl.Emit(OpCodes.Ldarg_0)
        cIl.Emit(OpCodes.Ldstr, value)
        cIl.Emit(OpCodes.Stfld, fieldBuilder)



    End Sub

End Class




