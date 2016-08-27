Imports System.ComponentModel
Imports PrimeBrick.MessageQueue.Common

Public Class ServerLogEventArgs
    Inherits AsyncCompletedEventArgs

    Public ReadOnly Property Message As String
    Public ReadOnly Property Severity As LogSeverity

    Public Sub New(Message As String, Severity As LogSeverity)
        MyBase.New(Nothing, False, Nothing)
        Me.Message = Message
        Me.Severity = Severity
    End Sub

End Class

Public Class ReceivedMessageArgs
    Inherits AsyncCompletedEventArgs

    Public ReadOnly Property Message As Message

    Public Sub New(Message As Message)
        MyBase.New(Nothing, False, Nothing)
        Me.Message = Message
    End Sub

End Class

Public Enum LogSeverity
    [Error] = 0
    Warning = 1
    Information = 2
End Enum