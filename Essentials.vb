Imports System.Drawing.Drawing2D

Public Module Essentials
    Public Property GameRunning As Boolean = True
    Public Property Score As Integer = 0
    Public Property StartTime As Date = Date.Now
    Public Property EndTime As Date? = Nothing
    Public Property Difficulty As Integer = 1
    Public Property EnemySpawnTimer As Integer = 0
    Public Property EnemySpawnInterval As Integer = 120
    Public ReadOnly Property RNG As New Random

    Public Function HslToRgb(h As Single, s As Single, l As Single) As Color
        s /= 100
        l /= 100
        Dim c = (1 - Math.Abs(2 * l - 1)) * s
        Dim x = c * (1 - Math.Abs((h / 60) Mod 2 - 1))
        Dim m = l - c / 2
        Dim r = 0, g = 0, b = 0

        If 0 <= h AndAlso h < 60 Then
            r = c
            g = x
        ElseIf 60 <= h AndAlso h < 120 Then
            r = x
            g = c
        ElseIf 120 <= h AndAlso h < 180 Then
            r = 0
            g = c
            b = x
        ElseIf 180 <= h AndAlso h < 240 Then
            g = x
            b = c
        ElseIf 240 <= h AndAlso h < 300 Then
            r = x
            b = c
        ElseIf 300 <= h AndAlso h < 360 Then
            r = c
            b = x
        End If

        r = CInt((r + m) * 255)
        g = CInt((g + m) * 255)
        b = CInt((b + m) * 255)
        Return Color.FromArgb(r, g, b)
    End Function
End Module

Friend Interface ICircle
    Property X As Single
    Property Y As Single
    Property Radius As Single
    Sub Draw(g As Graphics)
End Interface

Friend Class Player
    Implements ICircle

    Public Property X As Single Implements ICircle.X
    Public Property Y As Single Implements ICircle.Y
    Public Property Radius As Single Implements ICircle.Radius
    Public Property Color As Color

    Public Sub New(startX As Integer, startY As Integer, clientSize As Size)
        X = startX
        Y = startY
        Radius = Math.Min(clientSize.Width, clientSize.Height) * 0.05
        Color = Color.FromArgb(135, 206, 235)
    End Sub

    Public Sub Resize(clientSize As Size)
        Radius = Math.Min(clientSize.Width, clientSize.Height) * 0.05
    End Sub

    Public Sub Update(targetX As Integer, targetY As Integer)
        ' Move towards target position
        Dim dx As Single = targetX - X
        Dim dy As Single = targetY - Y
        Dim distance As Single = Math.Sqrt(dx * dx + dy * dy)

        If distance > 1 Then
            Dim moveX As Single = (dx / distance) * 5
            Dim moveY As Single = (dy / distance) * 5
            X += moveX
            Y += moveY
        End If

        ' Keep player within screen bounds
        X = Math.Clamp(X, Radius, Form.ActiveForm.ClientSize.Width - Radius)
        Y = Math.Clamp(Y, Radius, Form.ActiveForm.ClientSize.Height - Radius)
    End Sub

    Public Sub Draw(g As Graphics) Implements ICircle.Draw
        ' Glow effect
        Using glowBrush As New SolidBrush(Color.FromArgb(80, Color.R, Color.G, Color.B))
            Using path As New GraphicsPath
                path.AddEllipse(X - Radius - 10, Y - Radius - 10, (Radius + 10) * 2, (Radius + 10) * 2)
                g.FillPath(glowBrush, path)
            End Using
        End Using

        ' Main body
        Using bodyBrush As New SolidBrush(Color)
            g.FillEllipse(bodyBrush, X - Radius, Y - Radius, Radius * 2, Radius * 2)
        End Using

        ' Highlight effect
        Using highlightBrush As New SolidBrush(Color.FromArgb(80, 255, 255, 255))
            Dim highlightRadius As Single = Radius * 0.4
            g.FillEllipse(highlightBrush, X - Radius * 0.3F - highlightRadius, Y - Radius * 0.3F - highlightRadius, highlightRadius * 2, highlightRadius * 2)
        End Using

        ' Eyes
        Using eyeBrush As New SolidBrush(Color.White)
            Dim eyeWidth As Integer = 3
            Dim eyeHeight As Integer = Radius * 0.6
            Dim eyeYPos As Integer = Y - eyeHeight / 2

            ' Left eye
            g.FillRectangle(eyeBrush, X - Radius * 0.5F, eyeYPos, eyeWidth, eyeHeight)
            ' Right eye
            g.FillRectangle(eyeBrush, X + Radius * 0.3F, eyeYPos, eyeWidth, eyeHeight)
        End Using
    End Sub
End Class

Friend NotInheritable Class Enemy
    Implements ICircle

    Public Property X As Single Implements ICircle.X
    Public Property Y As Single Implements ICircle.Y
    Public Property Radius As Single Implements ICircle.Radius
    Public Property Speed As Single
    Public Property VelX As Single
    Public Property VelY As Single
    Public Property Color As Color

    Public Sub New(player As Player, difficulty As Integer, clientSize As Size)
        ' Spawn from random side
        Dim side As Integer = RNG.Next(0, 4)
        Select Case side
            Case 0 ' Top
                X = RNG.NextDouble() * clientSize.Width
                Y = -20
            Case 1 ' Right
                X = clientSize.Width + 20
                Y = RNG.NextDouble() * clientSize.Height
            Case 2 ' Bottom
                X = RNG.NextDouble() * clientSize.Width
                Y = clientSize.Height + 20
            Case 3 ' Left
                X = -20
                Y = RNG.NextDouble() * clientSize.Height
        End Select

        Radius = 15 + RNG.NextDouble() * 20
        Speed = 1 + RNG.NextDouble() * 2 + difficulty * 0.3

        ' Calculate velocity towards player
        Dim angle As Double = Math.Atan2(player.Y - Y, player.X - X)
        VelX = Math.Cos(angle) * Speed
        VelY = Math.Sin(angle) * Speed

        ' Random color using HSL
        Dim hue As Integer = RNG.Next(0, 60)
        Color = HSLToRGB(hue, 100, 50)
    End Sub

    Public Sub Update(player As Player)
        ' Update position
        X += VelX
        Y += VelY

        ' Track player
        Dim angle As Double = Math.Atan2(player.Y - Y, player.X - X)
        VelX = Math.Cos(angle) * Speed
        VelY = Math.Sin(angle) * Speed
    End Sub

    Public Sub Draw(g As Graphics) Implements ICircle.Draw
        ' Glow effect
        Using glowBrush As New SolidBrush(Color.FromArgb(80, Color.R, Color.G, Color.B))
            Using path As New GraphicsPath()
                path.AddEllipse(X - Radius - 10, Y - Radius - 10, (Radius + 10) * 2, (Radius + 10) * 2)
                g.FillPath(glowBrush, path)
            End Using
        End Using

        ' Main body
        Using bodyBrush As New SolidBrush(Color)
            g.FillEllipse(bodyBrush, X - Radius, Y - Radius, Radius * 2, Radius * 2)
        End Using
    End Sub
End Class

Friend NotInheritable Class Particle
    Public Property X As Single
    Public Property Y As Single
    Public Property VelX As Single
    Public Property VelY As Single
    Public Property Radius As Single
    Public Property Color As Color
    Public Property Alpha As Single
    Private Const DECAY As Single = 0.02

    Public Sub New(startX As Single, startY As Single, particleColor As Color)
        X = startX
        Y = startY
        VelX = (RNG.NextDouble() - 0.5) * 8
        VelY = (RNG.NextDouble() - 0.5) * 8
        Radius = RNG.NextDouble() * 3 + 1
        Color = particleColor
        Alpha = 1.0
    End Sub

    Public Sub Update()
        X += VelX
        Y += VelY
        Alpha -= DECAY
    End Sub

    Public Sub Draw(g As Graphics)
        If Alpha <= 0 Then Exit Sub

        Using brush As New SolidBrush(Color.FromArgb(Alpha * 255, Color.R, Color.G, Color.B))
            g.FillEllipse(brush, X - Radius, Y - Radius, Radius * 2, Radius * 2)
        End Using
    End Sub
End Class