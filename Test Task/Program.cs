using Leopotam.Ecs;
using System.Threading;

public abstract class SceneObject
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Id { get; set; }

    public abstract void Update();
}

public class Wall : SceneObject
{
    public (float startX, float startY) Start { get; set; }
    public (float endX, float endY) End { get; set; }
    
    /// <summary>
    /// Экземпляр объекта стена.
    /// </summary>
    /// <param name="id">Айди стены</param>
    /// <param name="startX">Стартовая координата X</param>
    /// <param name="startY">Стартовая координата Y</param>
    /// <param name="endX">Конечная координата X</param>
    /// <param name="endY">Конечная координата Y</param>
    public Wall(int id, float startX, float startY, float endX, float endY)
    {
        Id = id;
        Start = (startX, startY);
        End = (endX, endY);
    }

    public override void Update() { }
}

public class Ball : SceneObject
{
    public (float X, float Y) Position { get; set; }
    public (float speedX, float speedY) Velocity { get; set; }
    public float Radius { get; set; }

    /// <summary>
    /// Экземпляр объекта шар
    /// </summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="radius">Радиус шара.</param>
    /// <param name="speedX">Скорость по оси X.</param>
    /// <param name="speedY">Скорость по оси Y,</param>
    public Ball(float x, float y, float radius, float speedX, float speedY)
    {
        Position = (x, y);
        Radius = radius;
        Velocity = (speedX, speedY);
    }

    public override void Update()
    {
        Position = (Position.X + Velocity.speedX, Position.Y + Velocity.speedY);
    }
}

public class Scene
{
    public List<SceneObject> objects { get; private set; } = new List<SceneObject>();

    public Scene()
    {
        objects.Add(new Wall(1, 0, 0, 10, 0));
        objects.Add(new Wall(2, 10, 0, 10, -10));
        objects.Add(new Wall(3, 10, -10, 0, -10));
        objects.Add(new Wall(4, 0, -10, 0, 0));

        objects.Add(new Ball(5, -5, 2, 1, 0.6f));
    }
}

public class EcsWorldManager
{
    private EcsWorld _world;
    private EcsSystems _systems;

    public EcsWorldManager(Scene scene)
    {
        _world = new EcsWorld();
        _systems = new EcsSystems(_world);

        foreach (var obj in scene.objects)
        {
            var entity = _world.NewEntity();
            if (obj is Ball ball)
            {
                entity.Replace(new BallComponent { Ball = ball });
            }
            else if (obj is Wall wall)
            {
                entity.Replace(new WallComponent { Wall = wall });
            }
        }

        _systems
            .Add(new MoveSystem())
            .Add(new BounceSystem())
            .Init();
    }

    public void Update()
    {
        _systems.Run();
    }

    public void Destroy()
    {
        _systems.Destroy();
        _world.Destroy();
    }
}


public struct BallComponent
{
    public Ball Ball;
}

public struct WallComponent
{
    public Wall Wall;
}

public class MoveSystem : IEcsRunSystem
{
    private EcsFilter<BallComponent> _ballFilter;
    public void Run()
    {
        foreach (var entity in _ballFilter)
        {
            ref var ballComponent = ref _ballFilter.Get1(entity);
            ballComponent.Ball.Update();
            Console.WriteLine($"Ball position: ({ballComponent.Ball.Position.X}, {ballComponent.Ball.Position.Y})");
        }
    }
}

public class BounceSystem : IEcsRunSystem
{
    private EcsFilter<BallComponent> _ballFilter;
    private EcsFilter<WallComponent> _wallFilter;

    public void Run()
    {
        foreach (var ballEntity in _ballFilter)
        {
            ref var ballComponent = ref _ballFilter.Get1(ballEntity);
            var ball = ballComponent.Ball;

            foreach (var wallEntity in _wallFilter)
            {
                ref var wallComponent = ref _wallFilter.Get1(wallEntity);
                var wall = wallComponent.Wall;

                if (CheckCollision(ball, wall))
                {
                    ReflectVelocity(ball, wall);
                    Console.WriteLine($"Ball bounced off wall {wall.Id}");
                }
            }
        }
    }
    /// <summary>
    /// Проверяем ударился ли мяч об стенку, сравнивая перпендикуляр, опущенный из центра шара на стенку, с радиусом шара.
    /// </summary>
    /// <param name="ball"></param>
    /// <param name="wall"></param>
    /// <returns></returns>
    private bool CheckCollision(Ball ball, Wall wall)
    {
        var (startX, startY) = wall.Start;
        var (endX, endY) = wall.End;

        var (cx, cy) = ball.Position;
        var radius = ball.Radius;


        float dx = endX - startX;
        float dy = endY - startY;

        float fx = cx - startX;
        float fy = cy - startY;

        float t = (fx * dx + fy * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));

        float closestX = startX + t * dx;
        float closestY = startY + t * dy;

        float distance = (float)Math.Sqrt((closestX - cx) * (closestX - cx) + (closestY - cy) * (closestY - cy));

        return distance <= radius;
    }

    /// <summary>
    /// Задаем скорость отскока шарика.
    /// </summary>
    /// <param name="ball"></param>
    /// <param name="wall"></param>
    private void ReflectVelocity(Ball ball, Wall wall)
    {
        var (startX, startY) = wall.Start;
        var (endX, endY) = wall.End;

        float dx = endX - startX;
        float dy = endY - startY;

        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        float nx = -dy / length;
        float ny = dx / length;

        var (vx, vy) = ball.Velocity;

        float dot = vx * nx + vy * ny;
        ball.Velocity = (vx - 2 * dot * nx, vy - 2 * dot * ny);
    }
}

public class MainLoop
{
    public static void Run()
    {
        var scene = new Scene();
        var EcsWorldManager = new EcsWorldManager(scene);

        while (true)
        {
            EcsWorldManager.Update();
            System.Threading.Thread.Sleep(100);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        MainLoop.Run();
    }
}
