Public Class Message
    Public Property Type As MessageType
    Public Property Content As Object

    Sub New()
        Type = Nothing
        Content = Nothing
    End Sub

    Sub New(Type As MessageType, Content As Object)
        Me.Type = Type
        Me.Content = Content
    End Sub
End Class

Public Enum MessageType
    Publish = 0   'Publish \ Subscribe
    Request = 1   'Request \ Response
End Enum

