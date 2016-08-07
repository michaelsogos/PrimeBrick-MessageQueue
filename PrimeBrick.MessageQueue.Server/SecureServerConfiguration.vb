Imports System.Security.Authentication
Imports System.Security.Cryptography.X509Certificates

Public Class SecureServerConfiguration
    Public ReadOnly Property CertificationFilePath As String
    Public ReadOnly Property Certificate As X509Certificate2
    Public ReadOnly Property SecureProtocol As SslProtocols

    Sub New(ByVal SecureProtocol As SslProtocols, ByVal CertificationFilePath As String, Optional Password As String = Nothing)
        Me.CertificationFilePath = CertificationFilePath
        Me.SecureProtocol = SecureProtocol
        If Not IO.File.Exists(CertificationFilePath) Then Throw New Exception(String.Format("Cannot find the SSL certificate file on specified path [{0}]!", CertificationFilePath))
        If String.IsNullOrWhiteSpace(Password) Then
            Me.Certificate = New X509Certificate2(CertificationFilePath)
        Else
            Me.Certificate = New X509Certificate2(CertificationFilePath, Password)
        End If
    End Sub


End Class
