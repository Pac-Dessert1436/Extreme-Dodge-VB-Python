import pygame
import random
import math
from datetime import datetime
from abc import ABC, abstractmethod


class Circle(ABC):
    def __init__(self, x: float, y: float, radius: float, color: tuple) -> None:
        self.x = x
        self.y = y
        self.radius = radius
        self.color = color

    @abstractmethod
    def draw(self, screen: pygame.Surface) -> None:
        pass


# Initialize pygame
pygame.init()

# Set up display
WIDTH, HEIGHT = 800, 600
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption("Extreme Dodge")

# Colors
BLACK = (26, 26, 46)
LIGHT_BLUE = (135, 206, 235)
WHITE = (255, 255, 255)


class GameState:
    def __init__(self) -> None:
        self.game_running = True
        self.score = 0
        self.start_time = datetime.now()
        self.end_time: datetime | None = None
        self.difficulty = 1
        self.enemy_spawn_timer = 0
        self.enemy_spawn_interval = 120


class Player(Circle):
    def __init__(self) -> None:
        super().__init__(
            x=WIDTH // 2,
            y=HEIGHT // 2,
            radius=min(WIDTH, HEIGHT) * 0.05,
            color=(135, 206, 235, 180)
        )
        self.target_x = self.x
        self.target_y = self.y

    def update(self) -> None:
        # Move towards target position
        dx = self.target_x - self.x
        dy = self.target_y - self.y
        distance = math.sqrt(dx * dx + dy * dy)

        if distance > 1:
            move_x = (dx / distance) * 5
            move_y = (dy / distance) * 5
            self.x += move_x
            self.y += move_y

        # Keep player within screen bounds
        self.x = max(self.radius, min(WIDTH - self.radius, self.x))
        self.y = max(self.radius, min(HEIGHT - self.radius, self.y))

    def draw(self, screen: pygame.Surface) -> None:
        # Main body
        pygame.draw.circle(screen, self.color[:3], (int(
            self.x), int(self.y)), int(self.radius))

        # Glow effect
        glow_surface = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        glow_color = (*self.color[:3], 80)
        pygame.draw.circle(glow_surface, glow_color, (int(
            self.x), int(self.y)), int(self.radius) + 10)
        screen.blit(glow_surface, (0, 0), special_flags=pygame.BLEND_RGBA_ADD)

        # Highlight effect
        highlight_color = (255, 255, 255, 80)
        highlight_radius = int(self.radius * 0.4)
        pygame.draw.circle(screen, highlight_color[:3],
                           (int(self.x - self.radius * 0.3),
                            int(self.y - self.radius * 0.3)),
                           highlight_radius)

        # Eyes
        eye_width = 3
        eye_height = int(self.radius * 0.6)
        eye_y = int(self.y - eye_height / 2)

        # Eye glow
        eye_glow_color = (128, 128, 128, 80)
        pygame.draw.rect(screen, eye_glow_color[:3],
                         (int(self.x - self.radius * 0.5), eye_y, eye_width, eye_height))
        pygame.draw.rect(screen, eye_glow_color[:3],
                         (int(self.x + self.radius * 0.3), eye_y, eye_width, eye_height))


class Enemy(Circle):
    def __init__(self, player: Player, difficulty: int) -> None:
        # Spawn from random side
        side = random.randint(0, 3)
        if side == 0:  # Top
            self.x = random.randint(0, WIDTH)
            self.y = -20
        elif side == 1:  # Right
            self.x = WIDTH + 20
            self.y = random.randint(0, HEIGHT)
        elif side == 2:  # Bottom
            self.x = random.randint(0, WIDTH)
            self.y = HEIGHT + 20
        else:  # Left
            self.x = -20
            self.y = random.randint(0, HEIGHT)

        self.radius = random.randint(15, 35)
        self.speed = 1 + random.random() * 2 + difficulty * 0.3

        # Calculate initial velocity towards player
        angle = math.atan2(player.y - self.y, player.x - self.x)
        self.vx = math.cos(angle) * self.speed
        self.vy = math.sin(angle) * self.speed

        # Random color with HSL to RGB conversion
        hue = random.randint(0, 60)
        saturation = 100
        lightness = 50
        self.color = hsl_to_rgb(hue, saturation, lightness)

    def update(self, player: Player) -> None:
        # Update position
        self.x += self.vx
        self.y += self.vy

        # Track player
        angle = math.atan2(player.y - self.y, player.x - self.x)
        self.vx = math.cos(angle) * self.speed
        self.vy = math.sin(angle) * self.speed

    def draw(self, screen: pygame.Surface) -> None:
        # Enemy body with glow
        glow_surface = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        glow_color = (*self.color, 80)
        pygame.draw.circle(glow_surface, glow_color, (int(
            self.x), int(self.y)), int(self.radius) + 10)
        screen.blit(glow_surface, (0, 0), special_flags=pygame.BLEND_RGBA_ADD)

        # Main circle
        pygame.draw.circle(screen, self.color, (int(
            self.x), int(self.y)), int(self.radius))


class Particle:
    def __init__(self, x: float, y: float, color: tuple) -> None:
        self.x = x
        self.y = y
        self.vx = (random.random() - 0.5) * 8
        self.vy = (random.random() - 0.5) * 8
        self.radius = random.random() * 3 + 1
        self.color = color
        self.alpha = 1.0
        self.decay = 0.02

    def update(self) -> None:
        self.x += self.vx
        self.y += self.vy
        self.alpha -= self.decay

    def draw(self, screen: pygame.Surface) -> None:
        if self.alpha > 0:
            particle_color = (*self.color, int(self.alpha * 255))
            pygame.draw.circle(screen, particle_color[:3], (int(
                self.x), int(self.y)), int(self.radius))


def hsl_to_rgb(h: float, s: float, l: float) -> tuple:
    """Helper function to convert HSL to RGB"""
    s /= 100
    l /= 100
    c = (1 - abs(2 * l - 1)) * s
    x = c * (1 - abs((h / 60) % 2 - 1))
    m = l - c / 2
    r, g, b = 0, 0, 0

    if 0 <= h < 60:
        r, g, b = c, x, 0
    elif 60 <= h < 120:
        r, g, b = x, c, 0
    elif 120 <= h < 180:
        r, g, b = 0, c, x
    elif 180 <= h < 240:
        r, g, b = 0, x, c
    elif 240 <= h < 300:
        r, g, b = x, 0, c
    elif 300 <= h < 360:
        r, g, b = c, 0, x

    r = int((r + m) * 255)
    g = int((g + m) * 255)
    b = int((b + m) * 255)
    return (r, g, b)


def check_collision(circle1: Circle, circle2: Circle) -> bool:
    """Collision detection between two circles"""
    dx = circle1.x - circle2.x
    dy = circle1.y - circle2.y
    distance = math.sqrt(dx * dx + dy * dy)
    return distance < circle1.radius + circle2.radius


def create_explosion(x: float, y: float, color: tuple, particles: list) -> None:
    """Create explosion effect at given position"""
    for _ in range(20):
        particles.append(Particle(x, y, color))


def show_game_over(screen: pygame.Surface, score: int, survival_time: float, rank: str) -> None:
    """Show game over screen with final stats"""
    overlay = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
    overlay.fill((0, 0, 0, 200))
    screen.blit(overlay, (0, 0))

    font = pygame.font.SysFont("Courier New", 48, bold=True)
    small_font = pygame.font.SysFont("Courier New", 30, bold=True)

    # Title
    title_text = font.render("GAME OVER!", True, LIGHT_BLUE)
    title_rect = title_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 - 100))
    screen.blit(title_text, title_rect)

    # Stats
    score_text = small_font.render(f"Final Score: {score}", True, WHITE)
    score_rect = score_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 - 40))
    screen.blit(score_text, score_rect)

    time_text = small_font.render(
        f"Survival Time: {survival_time} seconds", True, WHITE)
    time_rect = time_text.get_rect(center=(WIDTH // 2, HEIGHT // 2))
    screen.blit(time_text, time_rect)

    rank_text = small_font.render(rank, True, LIGHT_BLUE)
    rank_rect = rank_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 + 40))
    screen.blit(rank_text, rank_rect)

    # Restart button
    restart_text = small_font.render("Press 'R' to Restart", True, WHITE)
    restart_rect = restart_text.get_rect(
        center=(WIDTH // 2, HEIGHT // 2 + 100))
    screen.blit(restart_text, restart_rect)


def get_rank(score: int) -> str:
    """Determine rank based on score"""
    if score < 500:
        return "Rank: Newbie Warrior"
    elif score < 1000:
        return "Rank: Dodge Apprentice"
    elif score < 2000:
        return "Rank: Movement Master"
    elif score < 5000:
        return "Rank: Extreme Survivor"
    else:
        return "Rank: Legendary Dodger"

# Main game function


def main() -> None:
    global WIDTH, HEIGHT

    # Initialize screen
    screen = pygame.display.set_mode((WIDTH, HEIGHT), pygame.RESIZABLE)

    # Initialize game objects
    game_state = GameState()
    player = Player()
    enemies: list[Enemy] = []
    particles: list[Particle] = []

    # Fonts
    score_font = pygame.font.SysFont("Courier New", 36, bold=True)
    info_font = pygame.font.SysFont("Courier New", 24, bold=True)

    # Clock for frame rate control
    clock = pygame.time.Clock()
    FPS = 60

    # Game loop
    running = True
    while running:
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.VIDEORESIZE:
                WIDTH, HEIGHT = event.size
                screen = pygame.display.set_mode(
                    (WIDTH, HEIGHT), pygame.RESIZABLE)
                player.radius = min(WIDTH, HEIGHT) * 0.05
                player.x = WIDTH // 2
                player.y = HEIGHT // 2
                player.target_x = player.x
                player.target_y = player.y
            elif event.type == pygame.MOUSEMOTION:
                if game_state.game_running:
                    player.target_x, player.target_y = pygame.mouse.get_pos()
            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_r and not game_state.game_running:
                    # Restart game
                    game_state = GameState()
                    player = Player()
                    enemies = []
                    particles = []

        # Clear screen with solid background
        screen.fill(BLACK)

        if game_state.game_running:
            # Update game objects
            player.update()

            # Spawn enemies
            game_state.enemy_spawn_timer += 1
            if game_state.enemy_spawn_timer >= game_state.enemy_spawn_interval:
                enemies.append(Enemy(player, game_state.difficulty))
                game_state.enemy_spawn_timer = 0
                game_state.enemy_spawn_interval = max(
                    30, 120 - game_state.difficulty * 10)

            # Update and draw enemies
            for enemy in enemies[:]:
                enemy.update(player)
                enemy.draw(screen)

                # Check collision with player
                if check_collision(player, enemy):
                    game_state.game_running = False
                    game_state.end_time = datetime.now()
                    create_explosion(player.x, player.y,
                                     player.color[:3], particles)

                # Remove enemies that go off screen
                if (enemy.x < -50 or enemy.x > WIDTH + 50 or
                        enemy.y < -50 or enemy.y > HEIGHT + 50):
                    enemies.remove(enemy)
                    game_state.score += 10

            # Check enemy collisions
            for i in range(len(enemies)-1, -1, -1):
                for j in range(i-1, -1, -1):
                    try:
                        enemy_i = enemies[i]
                        enemy_j = enemies[j]
                    except IndexError:
                        continue
                    if check_collision(enemy_i, enemy_j):
                        # Create explosion effects
                        create_explosion(
                            enemy_i.x, enemy_i.y, enemy_i.color[:3], particles)
                        create_explosion(
                            enemy_j.x, enemy_j.y, enemy_j.color[:3], particles)

                        # Remove both enemies
                        enemies.pop(i)
                        enemies.pop(j)
                        game_state.score += 20
                        break

            # Update and draw particles
            for particle in particles[:]:
                particle.update()
                particle.draw(screen)
                if particle.alpha <= 0:
                    particles.remove(particle)

            # Update score and difficulty
            game_state.score += 1
            game_state.difficulty = math.floor(game_state.score / 500) + 1

            # Draw player
            player.draw(screen)

            # Draw UI
            score_text = score_font.render(
                f"Score: {game_state.score}", True, WHITE)
            screen.blit(score_text, (20, 20))

            difficulty_text = info_font.render(
                f"Difficulty: {game_state.difficulty}", True, LIGHT_BLUE)
            screen.blit(difficulty_text, (20, 60))
        else:
            # Calculate survival time using end_time that was set when game ended
            if game_state.end_time is None:
                game_state.end_time = datetime.now()
            survival_time = math.floor(
                (game_state.end_time - game_state.start_time).total_seconds())

            # Get rank
            rank = get_rank(game_state.score)

            # Show game over screen
            show_game_over(screen, game_state.score, survival_time, rank)

        # Update display and control frame rate
        pygame.display.flip()
        clock.tick(FPS)

    pygame.quit()
    exit(0)


if __name__ == "__main__":
    main()
