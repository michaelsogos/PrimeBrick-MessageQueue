Imports System.ComponentModel

Public Class ServerLogEventArgs
    Inherits AsyncCompletedEventArgs

    Public ReadOnly Property Message As String
    Public ReadOnly Property Gravity As String

    Public Sub New(Message As String, Gravity As LogGravity)
        MyBase.New(Nothing, False, Nothing)
        Me.Message = Message
        Me.Gravity = Gravity
    End Sub

End Class

Public Enum LogGravity
    [Error] = 0
    Warning = 1
    Information = 2
End Enum