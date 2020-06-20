Imports System.IO

Module AutoUpdater
    Dim detailsFile As StreamWriter = Nothing
    Dim Extract_location As String = System.IO.Path.GetTempPath() & "CoffeecupUpdate"
    Public Function ExtractSpecificZipFile(ByVal AssemBlyName As String, ByVal UpdateFileName As String, ByVal version As String) As Boolean
        Try
            If (System.IO.Directory.Exists(Extract_location)) Then
                My.Computer.FileSystem.DeleteDirectory(Extract_location, FileIO.DeleteDirectoryOption.DeleteAllContents)
            End If
            If (Not System.IO.Directory.Exists(Extract_location)) Then
                System.IO.Directory.CreateDirectory(Extract_location)
            End If

            Dim unzip As New ICSharpCode.SharpZipLib.Zip.FastZip
            unzip.ExtractZip(UpdateFileName, Extract_location, Nothing)

            Return (True)
        Catch ex As Exception
            MessageBox.Show("Message:" & ex.Message & vbCrLf, _
                             "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try

    End Function


End Module
