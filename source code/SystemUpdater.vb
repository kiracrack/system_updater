Imports System
Imports System.IO
Imports System.ComponentModel
Imports System.Threading
Imports System.Reflection

Public Class SystemUpdater
    
    Dim resetEvent As New Threading.AutoResetEvent(False)
    Dim calcDoneEvent As New Threading.AutoResetEvent(True)
    Dim Extract_location As String = System.IO.Path.GetTempPath() & "CoffeecupUpdate"
    Dim Itemcount As Integer = 0 : Dim RetryCount As Integer = 1
    Dim AssemblyName As String = "" : Dim AssemblyLocation As String = "" : Dim UpdateFileName As String = "" : Dim Version As String = ""
    Delegate Sub LogUpdate(ByVal msg As String)
    Delegate Sub ProgressBar(ByVal val As Integer)
    Delegate Sub CompleteUpdate(ByVal completed As Boolean, ByVal remarks As String)

    Private Sub SystemUpdater_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ListView1.View = View.Details
        ListView1.Columns.Add("Update Details", ListView1.Width - 29, HorizontalAlignment.Left)

        Dim UpdateInfoPath As String = Application.StartupPath.ToString & "\UpdateVersion\UpdateInfo.txt"
        If System.IO.File.Exists(UpdateInfoPath) = True Then
            Dim ReadLines() As String = IO.File.ReadAllLines(UpdateInfoPath)
            Dim GetData() As String = ReadLines(0).ToString.Split("|"c)

            AssemblyName = GetData(0)
            AssemblyLocation = GetData(1)
            UpdateFileName = GetData(2)
            Version = GetData(3)

            ExtractSpecificZipFile(AssemblyName, UpdateFileName, Version)
            ProgressBar1.Maximum = System.IO.Directory.GetFiles(Extract_location, "*", System.IO.SearchOption.AllDirectories).Length
            Timer1.Start()
        Else
            MessageBox.Show("Update file not found!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End
        End If
    End Sub


    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Timer1.Stop()
        worker.WorkerReportsProgress = True
        worker.WorkerSupportsCancellation = True
        worker.RunWorkerAsync()
    End Sub

    Private Sub worker_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles worker.DoWork
        Dim safedelegate As New LogUpdate(AddressOf RecordUpdateLog)
        Dim delegateProgress As New ProgressBar(AddressOf UpdateProgressBar)
        Dim completeDelegate As New CompleteUpdate(AddressOf UpdateComplete)
        Try
            For Each p As Process In Process.GetProcesses
                If p.ProcessName = AssemblyName.Replace(".exe", "") Then
                    Me.Invoke(safedelegate, "Shutting down " & AssemblyName.Replace(".exe", "") & " System..")
                    Thread.Sleep(3000)
                    If Not p.HasExited Then
                        p.Kill() 'force the process to exit.
                        Me.Invoke(safedelegate, "Shutdown completed!")
                        Thread.Sleep(2000)
                    End If
                End If
            Next

            Dim fileSystemInfo As System.IO.FileSystemInfo
            Dim sourceDirectoryInfo As New System.IO.DirectoryInfo(Extract_location)
            For Each fileSystemInfo In sourceDirectoryInfo.GetFileSystemInfos
                Dim destinationFileName As String = System.IO.Path.Combine(AssemblyLocation, fileSystemInfo.Name)
                If TypeOf fileSystemInfo Is System.IO.FileInfo Then
                    If fileSystemInfo.Name <> "Thumbs.db" Then
                        System.IO.File.Copy(fileSystemInfo.FullName, destinationFileName, True)
                    End If
                    Me.Invoke(safedelegate, "Updating " & destinationFileName.Replace(AssemblyLocation, ""))
                    Me.Invoke(delegateProgress, Itemcount + 1)
                Else
                    CopyDirectory(fileSystemInfo.FullName, destinationFileName)
                End If

                If sender.CancellationPending Then
                    e.Cancel = True
                    Return
                End If
            Next
            'For Each Dir As String In System.IO.Directory.GetFiles(Extract_location, "*", System.IO.SearchOption.AllDirectories)
            '    Me.Invoke(safedelegate, "Updating " & Path.GetFileName(Dir), cnt + 1)
            '    CopyDirectory(Path.GetDirectoryName(Dir), AssemblyLocation)
            'Next
            Me.Invoke(safedelegate, "Update completed")
            Me.Invoke(completeDelegate, True, "Update Completed")

        Catch ex As Exception
            Me.Invoke(completeDelegate, False, ex.Message)
        End Try
    End Sub

    Public Sub CopyDirectory(ByVal sourcePath As String, ByVal destinationPath As String)
        Dim safedelegate As New LogUpdate(AddressOf RecordUpdateLog)
        Dim delegateProgress As New ProgressBar(AddressOf UpdateProgressBar)
        Dim sourceDirectoryInfo As New System.IO.DirectoryInfo(sourcePath)

        If Not System.IO.Directory.Exists(destinationPath) Then
            System.IO.Directory.CreateDirectory(destinationPath)
        End If

        Dim fileSystemInfo As System.IO.FileSystemInfo
        For Each fileSystemInfo In sourceDirectoryInfo.GetFileSystemInfos
            Dim destinationFileName As String = System.IO.Path.Combine(destinationPath, fileSystemInfo.Name)

            If TypeOf fileSystemInfo Is System.IO.FileInfo Then
                If fileSystemInfo.Name <> "Thumbs.db" Then
                    System.IO.File.Copy(fileSystemInfo.FullName, destinationFileName, True)
                End If
                Me.Invoke(safedelegate, "Updating " & destinationFileName.Replace(AssemblyLocation, ""))
                Me.Invoke(delegateProgress, Itemcount + 1)
            Else
                CopyDirectory(fileSystemInfo.FullName, destinationFileName)
            End If
        Next
    End Sub

    Public Sub UpdateComplete(ByVal UpdateComplete As Boolean, ByVal remarks As String)
        If UpdateComplete = False Then
            If RetryCount > 3 Then
                If MessageBox.Show("System unable to proceed update due to error persevere many times! System is requesting you to restart your pc!" & Environment.NewLine & Environment.NewLine _
                                            + "Are you sure you want to restart your pc?", "System Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) = vbYes Then
                    Dim wsh As Object = CreateObject("WScript.Shell")
                    Dim MyShortcut
                    MyShortcut = wsh.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) & "\" & AssemblyName.Replace(".exe", "") & ".lnk")
                    MyShortcut.TargetPath = wsh.ExpandEnvironmentStrings(Assembly.GetExecutingAssembly().Location)
                    MyShortcut.WorkingDirectory = wsh.ExpandEnvironmentStrings(Assembly.GetExecutingAssembly().Location)
                    MyShortcut.WindowStyle = 4
                    MyShortcut.Save()

                    System.Diagnostics.Process.Start("shutdown", "-r -t 00")
                    End
                Else
                    End
                End If
            Else
                If MessageBox.Show(remarks, "System Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) = vbRetry Then
                    Itemcount = 0 : RetryCount += 1
                    Timer1.Start()
                Else
                    End
                End If
            End If
        Else
            Me.Hide()
            MessageBox.Show("Your system successfully updated! To view update features, please check your email notification for more details", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Process.Start(AssemblyLocation & "\" & AssemblyName)
            File.Delete(Application.StartupPath.ToString & "\UpdateVersion\UpdateInfo.txt")
            If File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) & "\" & AssemblyName.Replace(".exe", "") & ".lnk") Then
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) & "\" & AssemblyName.Replace(".exe", "") & ".lnk")
            End If
            Me.Close()
        End If
    End Sub

    Public Sub RecordUpdateLog(ByVal details As String)
        Dim item1 As New ListViewItem(details, 0)
        ListView1.Items.AddRange(New ListViewItem() {item1})
        ListView1.Items(ListView1.Items.Count - 1).EnsureVisible()
    End Sub

    Public Sub UpdateProgressBar(ByVal cnt As Integer)
        Itemcount = Itemcount + 1
        Me.ProgressBar1.Value = Itemcount
    End Sub
    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        If CheckBox1.Checked = True Then
            CheckBox1.Checked = False
            LinkLabel1.Text = "Show update details.."
            ListView1.Visible = False
            Me.Size = New Size(Me.Width, 149)
        Else
            CheckBox1.Checked = True
            LinkLabel1.Text = "Hide update details.."
            ListView1.Visible = True
            Me.Size = New Size(Me.Width, 396)
        End If
    End Sub
End Class