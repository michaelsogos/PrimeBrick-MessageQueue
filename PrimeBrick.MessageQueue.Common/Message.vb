Imports PrimeBrick.MessageQueue.Common.Enums

Public Class Message
    Public Property Type As MessageType
    Public Property Content As Object
    Public Property QueueName As String

    Sub New()
        Type = Nothing
        Content = Nothing
        QueueName = Nothing
    End Sub

    Sub New(QueueName As String, Type As MessageType, Content As Object)
        Me.Type = Type
        Me.Content = Content
        Me.QueueName = QueueName
    End Sub
End Class



