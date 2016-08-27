Namespace Enums
    ''' <summary>
    ''' The message type. It can be PUB-SUB or REQ-RES, refear to documentation for more details.
    ''' </summary>
    Public Enum MessageType
        Publish = 0   'Publish \ Subscribe
        Request = 1   'Request \ Response
    End Enum

    ''' <summary>
    ''' The log severity
    ''' </summary>
    <Flags>
    Public Enum LogSeverity
        [Error] = 0
        Warning = 1
        Information = 2
        [Debug] = 4
    End Enum
End Namespace