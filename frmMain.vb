Imports System.Drawing.Drawing2D

#Disable Warning IDE1006
Public Class frmMain
    ' Game constants
    Private Const FPS As Integer = 60
    Private Const BASE_SPAWN_INTERVAL As Integer = 120

    ' Game state
    Private player As Player
    Private enemies As List(Of Enemy)
    Private particles As List(Of Particle)

    ' Game loop timer
    Private WithEvents Timer As New Timer With {.Interval = 1000 \ FPS}
    Private mouseX As Integer = 400
    Private mouseY As Integer = 300

    ' Fonts for UI elements
    Private Const FONT_NAME = "Courier New"
    Private ReadOnly Property ScoreFont As New Font(FONT_NAME, 25, FontStyle.Bold)
    Private ReadOnly Property InfoFont As New Font(FONT_NAME, 22, FontStyle.Bold)
    Private ReadOnly Property GameOverFont As New Font(FONT_NAME, 28, FontStyle.Bold)
    Private ReadOnly Property SmallFont As New Font(FONT_NAME, 20, FontStyle.Bold)

    Public Sub New()
        InitializeComponent()
        InitializeGame()
    End Sub

    Private Sub InitializeGame()
        ' Set form properties
        DoubleBuffered = True
        BackColor = Color.FromArgb(26, 26, 46)

        ' Initialize game objects and start game loop timer
        ResetGame()
        Timer.Start()
    End Sub

    Private Sub ResetGame()
        ' Reset global game state
        GameRunning = True
        Score = 0
        StartTime = Date.Now
        EndTime = Nothing
        Difficulty = 1
        EnemySpawnTimer = 0
        EnemySpawnInterval = 120

        ' Reset game objects
        player = New Player(ClientSize.Width \ 2, ClientSize.Height \ 2, ClientSize)
        enemies = New List(Of Enemy)
        particles = New List(Of Particle)
    End Sub

    Private Sub GameLoop(sender As Object, e As EventArgs) Handles Timer.Tick
        If GameRunning Then UpdateGame()
        Invalidate()
    End Sub

    Private Sub UpdateGame()
        ' Update player
        player.Update(mouseX, mouseY)

        ' Spawn enemies
        EnemySpawnTimer += 1
        If EnemySpawnTimer >= EnemySpawnInterval Then
            enemies.Add(New Enemy(player, Difficulty, ClientSize))
            EnemySpawnTimer = 0
            EnemySpawnInterval = Math.Max(30, BASE_SPAWN_INTERVAL - Difficulty * 10)
        End If

        ' Update and check enemies
        For i As Integer = enemies.Count - 1 To 0 Step -1
            Dim enemy As Enemy = enemies(i)
            enemy.Update(player)

            ' Check collision with player
            If CheckCollision(player, enemy) Then
                GameRunning = False
                EndTime = Date.Now
                CreateExplosion(player.X, player.Y, player.Color, particles)
                Exit For
            End If

            ' Remove enemies that go off screen
            If enemy.X < -50 OrElse enemy.X > ClientSize.Width + 50 OrElse
               enemy.Y < -50 OrElse enemy.Y > ClientSize.Height + 50 Then
                enemies.RemoveAt(i)
                Score += 10
            End If
        Next i

        Dim TryIndex = Function(idx As Integer, ByRef enemy As Enemy) As Boolean
                           Try
                               enemy = enemies(idx)
                               Return True
                           Catch
                               enemy = Nothing
                               Return False
                           End Try
                       End Function

        ' Check enemy collisions
        For i As Integer = enemies.Count - 1 To 1 Step -1
            For j As Integer = i - 1 To 0 Step -1
                Dim enemy1 As Enemy = Nothing, enemy2 As Enemy = Nothing
                If Not (TryIndex(i, enemy1) AndAlso TryIndex(j, enemy2)) Then Continue For

                If CheckCollision(enemy1, enemy2) Then
                    CreateExplosion(enemy1.X, enemy1.Y, enemy1.Color, particles)
                    CreateExplosion(enemy2.X, enemy2.Y, enemy2.Color, particles)
                    enemies.RemoveAt(i)
                    enemies.RemoveAt(j)
                    Score += 20
                    Exit For
                End If
            Next j
        Next i

        ' Update particles
        For i As Integer = particles.Count - 1 To 0 Step -1
            Dim particle As Particle = particles(i)
            particle.Update()
            If particle.Alpha <= 0 Then particles.RemoveAt(i)
        Next i

        ' Update score and difficulty
        Score += 1
        Difficulty = (Score \ 500) + 1
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics

        ' Set high quality rendering
        g.SmoothingMode = SmoothingMode.HighQuality
        g.InterpolationMode = InterpolationMode.HighQualityBicubic

        If GameRunning Then
            ' Draw particles
            For Each particle As Particle In particles : particle.Draw(g) : Next particle
            ' Draw enemies
            For Each enemy As Enemy In enemies : enemy.Draw(g) : Next enemy
            ' Draw player
            player.Draw(g)

            ' Draw UI
            Using brush As New SolidBrush(Color.White)
                g.DrawString("Score: " & Score, scoreFont, brush, 20, 20)
            End Using

            Using brush As New SolidBrush(Color.FromArgb(135, 206, 235))
                g.DrawString("Difficulty: " & Difficulty, infoFont, brush, 20, 70)
            End Using
        Else
            ' Draw game over screen
            DrawGameOver(g)
        End If
    End Sub

    Private Sub DrawGameOver(g As Graphics)
        ' Dark overlay
        Using overlayBrush As New SolidBrush(Color.FromArgb(200, 0, 0, 0))
            g.FillRectangle(overlayBrush, ClientRectangle)
        End Using

        ' Calculate survival time
        Dim survivalTime = If(EndTime IsNot Nothing, CInt((EndTime.Value - StartTime).TotalSeconds), 0)

        ' Draw game over text
        Using blueBrush As New SolidBrush(Color.FromArgb(135, 206, 235))
            Using whiteBrush As New SolidBrush(Color.White)
                ' Title text
                Const TITLE_TEXT As String = "GAME OVER!"
                Dim titleSize As SizeF = g.MeasureString(TITLE_TEXT, GameOverFont)
                g.DrawString(TITLE_TEXT, GameOverFont, blueBrush,
                    (ClientSize.Width - titleSize.Width) / 2,
                    (ClientSize.Height - titleSize.Height) / 2 - 120)

                ' Statistics
                Dim scoreText As String = "Final Score: " & Score
                Dim scoreSize As SizeF = g.MeasureString(scoreText, SmallFont)
                g.DrawString(scoreText, SmallFont, whiteBrush,
                    (ClientSize.Width - scoreSize.Width) / 2,
                    (ClientSize.Height - scoreSize.Height) / 2 - 30)

                Dim timeText As String = "Survival Time: " & survivalTime & " seconds"
                Dim timeSize As SizeF = g.MeasureString(timeText, SmallFont)
                g.DrawString(timeText, SmallFont, whiteBrush,
                    (ClientSize.Width - timeSize.Width) / 2,
                    (ClientSize.Height - timeSize.Height) / 2 + 20)

                ' Rank (based on score)
                Dim rank As String = GetRank(Score)
                Dim rankSize As SizeF = g.MeasureString(rank, SmallFont)
                g.DrawString(rank, SmallFont, blueBrush,
                    (ClientSize.Width - rankSize.Width) / 2,
                    (ClientSize.Height - rankSize.Height) / 2 + 70)

                ' Restart instruction
                Const RESTART_TEXT As String = "Press 'R' to Restart"
                Dim restartSize As SizeF = g.MeasureString(RESTART_TEXT, SmallFont)
                g.DrawString(RESTART_TEXT, SmallFont, whiteBrush,
                    (ClientSize.Width - restartSize.Width) / 2,
                    (ClientSize.Height - restartSize.Height) / 2 + 150)
            End Using
        End Using
    End Sub

    Private Function GetRank(score As Integer) As String
        Select Case score
            Case Is < 500
                Return "Rank: Newbie Warrior"
            Case Is < 1000
                Return "Rank: Dodge Apprentice"
            Case Is < 2000
                Return "Rank: Movement Master"
            Case Is < 5000
                Return "Rank: Extreme Survivor"
            Case Else
                Return "Rank: Legendary Dodger"
        End Select
    End Function

    Private Sub CreateExplosion(x As Single, y As Single, color As Color, particles As List(Of Particle))
        For i As Integer = 1 To 20 : particles.Add(New Particle(x, y, color)) : Next i
    End Sub

    Private Function CheckCollision(circle1 As ICircle, circle2 As ICircle) As Boolean
        Dim dx As Single = circle1.X - circle2.X
        Dim dy As Single = circle1.Y - circle2.Y
        Dim distance As Single = Math.Sqrt(dx * dx + dy * dy)
        Return distance < circle1.Radius + circle2.Radius
    End Function

    ' Mouse movement tracking
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        mouseX = e.X
        mouseY = e.Y
    End Sub

    ' Handle key press for restart
    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        MyBase.OnKeyPress(e)
        If e.KeyChar = "r"c OrElse e.KeyChar = "R"c AndAlso Not GameRunning Then ResetGame()
    End Sub

    ' Handle form resize
    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        player?.Resize(ClientSize)
    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Center mouse initially
        mouseX = ClientSize.Width \ 2
        mouseY = ClientSize.Height \ 2
    End Sub

    <STAThread> Friend Shared Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New frmMain)
    End Sub
End Class