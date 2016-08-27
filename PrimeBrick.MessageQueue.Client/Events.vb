Imports System.ComponentModel
Imports PrimeBrick.MessageQueue.Common.Enums

Public Class ClientLogEventArgs
    Inherits AsyncCompletedEventArgs

    Public ReadOnly Property Message As String
    Public ReadOnly Property Severity As LogSeverity

    Public Sub New(Message As String, Severity As LogSeverity)
        MyBase.New(Nothing, False, Nothing)
        Me.Message = Message
        Me.Severity = Severity
    End Sub
End Class