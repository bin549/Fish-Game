using Godot;
using System;

public partial class Game : Node2D
{
    private readonly RandomNumberGenerator _random = new();
    private Node2D _fishLayer = new();
    private Node2D _foodLayer = new();
    private Node2D _bubbleLayer = new();
    private double _bubbleTimer;
    private float _time;

    private readonly Color[] _fishPalette =
    {
        new(1.00f, 0.55f, 0.18f),
        new(0.23f, 0.74f, 1.00f),
        new(0.98f, 0.86f, 0.28f),
        new(0.94f, 0.32f, 0.51f),
        new(0.36f, 0.88f, 0.62f),
    };

    public override void _Ready()
    {
        _random.Randomize();

        _foodLayer.Name = "Food";
        _fishLayer.Name = "Fish";
        _bubbleLayer.Name = "Bubbles";

        AddChild(_foodLayer);
        AddChild(_fishLayer);
        AddChild(_bubbleLayer);

        var viewportSize = GetViewportRect().Size;
        if (viewportSize == Vector2.Zero)
        {
            viewportSize = new Vector2(1280, 720);
        }

        for (var i = 0; i < 11; i++)
        {
            var fish = new RoamingFish();
            fish.Configure(
                _fishPalette[i % _fishPalette.Length],
                _random.RandfRange(0.78f, 1.24f),
                _random.RandfRange(76.0f, 132.0f),
                _random.Randi());
            fish.Position = RandomPointInside(viewportSize, 96.0f);
            _fishLayer.AddChild(fish);
        }

        for (var i = 0; i < 16; i++)
        {
            SpawnBubble(true);
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _bubbleTimer -= delta;

        if (_bubbleTimer <= 0.0)
        {
            SpawnBubble(false);
            _bubbleTimer = _random.RandfRange(0.22f, 0.72f);
        }

        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse ||
            !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        var pellet = new FoodPellet();
        pellet.Position = mouse.Position;
        _foodLayer.AddChild(pellet);
    }

    public override void _Draw()
    {
        var size = GetViewportRect().Size;
        if (size == Vector2.Zero)
        {
            size = new Vector2(1280, 720);
        }

        DrawWater(size);
        DrawSand(size);
        DrawPlants(size);
    }

    private Vector2 RandomPointInside(Vector2 size, float margin)
    {
        return new Vector2(
            SafeRandfRange(margin, size.X - margin),
            SafeRandfRange(margin, size.Y - margin - 90.0f));
    }

    private void SpawnBubble(bool anywhere)
    {
        var size = GetViewportRect().Size;
        if (size == Vector2.Zero)
        {
            size = new Vector2(1280, 720);
        }

        var bubble = new Bubble();
        bubble.Configure(
            _random.RandfRange(5.0f, 15.0f),
            _random.RandfRange(28.0f, 72.0f),
            _random.RandfRange(-18.0f, 18.0f),
            _random.Randf());
        bubble.Position = new Vector2(
            SafeRandfRange(24.0f, size.X - 24.0f),
            anywhere ? SafeRandfRange(32.0f, size.Y - 42.0f) : size.Y + 24.0f);
        _bubbleLayer.AddChild(bubble);
    }

    private float SafeRandfRange(float from, float to)
    {
        if (from <= to)
        {
            return _random.RandfRange(from, to);
        }

        return (from + to) * 0.5f;
    }

    private void DrawWater(Vector2 size)
    {
        var top = new Color(0.04f, 0.40f, 0.64f);
        var bottom = new Color(0.00f, 0.18f, 0.31f);
        const int bands = 24;

        for (var i = 0; i < bands; i++)
        {
            var t = i / (float)(bands - 1);
            var color = top.Lerp(bottom, t);
            var y = size.Y * t;
            DrawRect(new Rect2(0.0f, y, size.X, size.Y / bands + 1.0f), color);
        }

        for (var i = 0; i < 8; i++)
        {
            var x = (i + 0.45f) * size.X / 8.0f + Mathf.Sin(_time * 0.52f + i) * 18.0f;
            var ray = new Color(0.62f, 0.91f, 1.0f, 0.055f);
            DrawPolygon(
                new[]
                {
                    new Vector2(x - 18.0f, 0.0f),
                    new Vector2(x + 28.0f, 0.0f),
                    new Vector2(x + 86.0f, size.Y),
                    new Vector2(x - 82.0f, size.Y),
                },
                new[] { ray, ray, new Color(ray, 0.0f), new Color(ray, 0.0f) });
        }
    }

    private void DrawSand(Vector2 size)
    {
        var sandTop = size.Y - 74.0f;
        DrawRect(new Rect2(0.0f, sandTop, size.X, 74.0f), new Color(0.77f, 0.61f, 0.36f));
        DrawRect(new Rect2(0.0f, sandTop, size.X, 12.0f), new Color(0.89f, 0.74f, 0.43f, 0.72f));

        for (var i = 0; i < 42; i++)
        {
            var x = (i * 97.0f) % Mathf.Max(1.0f, size.X);
            var y = sandTop + 16.0f + (i * 29.0f) % 48.0f;
            var radius = 1.8f + i % 5;
            DrawCircle(new Vector2(x, y), radius, new Color(0.42f, 0.35f, 0.28f, 0.42f));
        }
    }

    private void DrawPlants(Vector2 size)
    {
        var sandTop = size.Y - 72.0f;

        for (var cluster = 0; cluster < 9; cluster++)
        {
            var rootX = (cluster + 0.5f) * size.X / 9.0f;
            var height = 54.0f + cluster % 4 * 18.0f;
            var strands = 3 + cluster % 3;

            for (var strand = 0; strand < strands; strand++)
            {
                var phase = cluster * 0.9f + strand * 1.7f;
                var baseX = rootX + (strand - strands * 0.5f) * 11.0f;
                var tip = new Vector2(
                    baseX + Mathf.Sin(_time * 1.4f + phase) * 18.0f,
                    sandTop - height - Mathf.Sin(phase) * 10.0f);
                var mid = new Vector2(
                    baseX + Mathf.Sin(_time * 1.1f + phase) * 10.0f,
                    sandTop - height * 0.55f);

                DrawLine(new Vector2(baseX, sandTop + 4.0f), mid, new Color(0.14f, 0.48f, 0.27f), 5.0f);
                DrawLine(mid, tip, new Color(0.21f, 0.68f, 0.35f), 4.0f);
            }
        }
    }
}

public partial class RoamingFish : Node2D
{
    private readonly RandomNumberGenerator _random = new();
    private Color _bodyColor = new(1.0f, 0.55f, 0.18f);
    private Vector2 _velocity;
    private Vector2 _wanderTarget;
    private float _baseSpeed = 96.0f;
    private float _bodyScale = 1.0f;
    private float _facing = 1.0f;
    private float _turnSpeed = 3.2f;
    private float _reTargetTime;
    private float _swimClock;
    private ulong _seed;

    public void Configure(Color bodyColor, float bodyScale, float baseSpeed, ulong seed)
    {
        _bodyColor = bodyColor;
        _bodyScale = bodyScale;
        _baseSpeed = baseSpeed;
        _seed = seed;
    }

    public override void _Ready()
    {
        if (_seed == 0)
        {
            _random.Randomize();
        }
        else
        {
            _random.Seed = _seed;
        }

        AddToGroup("fish");
        ZIndex = 10;
        PickNewTarget();
        _velocity = (_wanderTarget - Position).Normalized() * _baseSpeed * 0.45f;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _swimClock += dt * (5.5f + _baseSpeed * 0.018f);
        _reTargetTime -= dt;

        var food = FindNearestFood();
        var target = food?.GlobalPosition ?? _wanderTarget;

        if (food is not null && GlobalPosition.DistanceTo(food.GlobalPosition) < 18.0f * _bodyScale)
        {
            food.QueueFree();
            PickNewTarget();
            target = _wanderTarget;
        }

        if (food is null && (_reTargetTime <= 0.0f || Position.DistanceTo(_wanderTarget) < 28.0f))
        {
            PickNewTarget();
            target = _wanderTarget;
        }

        var direction = target - GlobalPosition;
        if (direction.LengthSquared() > 1.0f)
        {
            direction = direction.Normalized();
        }

        var desiredSpeed = food is null ? _baseSpeed : _baseSpeed * 1.38f;
        var desiredVelocity = direction * desiredSpeed;
        var steerAmount = 1.0f - Mathf.Exp(-_turnSpeed * dt);
        _velocity = _velocity.Lerp(desiredVelocity, steerAmount);

        Position += _velocity * dt;
        KeepInsideViewport();

        if (Mathf.Abs(_velocity.X) > 8.0f)
        {
            var targetFacing = _velocity.X >= 0.0f ? 1.0f : -1.0f;
            _facing = Mathf.MoveToward(_facing, targetFacing, dt * 5.0f);
        }

        Scale = new Vector2(_facing * _bodyScale, _bodyScale);
        Rotation = Mathf.Lerp(Rotation, Mathf.Clamp(_velocity.Y / Mathf.Max(1.0f, _baseSpeed) * 0.16f, -0.18f, 0.18f), steerAmount);

        QueueRedraw();
    }

    public override void _Draw()
    {
        var body = EllipsePoints(Vector2.Zero, 36.0f, 20.0f, 32);
        var outline = EllipsePoints(Vector2.Zero, 38.0f, 22.0f, 32);
        var highlight = EllipsePoints(new Vector2(8.0f, -7.0f), 15.0f, 5.0f, 18);
        var tailWag = Mathf.Sin(_swimClock) * 8.0f;
        var finWag = Mathf.Sin(_swimClock + 0.7f) * 3.0f;

        DrawPolygon(outline, Fill(outline.Length, new Color(0.03f, 0.09f, 0.11f, 0.28f)));
        DrawPolygon(
            new[]
            {
                new Vector2(-28.0f, 0.0f),
                new Vector2(-63.0f, -22.0f + tailWag),
                new Vector2(-56.0f, 0.0f),
                new Vector2(-63.0f, 22.0f + tailWag),
            },
            Fill(4, _bodyColor.Darkened(0.18f)));

        DrawPolygon(body, Fill(body.Length, _bodyColor));
        DrawPolygon(
            new[]
            {
                new Vector2(-5.0f, 6.0f),
                new Vector2(-22.0f, 28.0f + finWag),
                new Vector2(12.0f, 12.0f),
            },
            Fill(3, _bodyColor.Darkened(0.24f)));
        DrawPolygon(highlight, Fill(highlight.Length, new Color(1.0f, 1.0f, 1.0f, 0.28f)));

        DrawCircle(new Vector2(22.0f, -6.0f), 4.2f, Colors.White);
        DrawCircle(new Vector2(23.5f, -6.0f), 1.9f, new Color(0.02f, 0.04f, 0.05f));
        DrawLine(new Vector2(27.0f, 5.0f), new Vector2(35.0f, 3.0f), new Color(0.20f, 0.07f, 0.07f, 0.6f), 1.7f);
    }

    private FoodPellet FindNearestFood()
    {
        FoodPellet best = null;
        var bestDistance = 190.0f * 190.0f;

        foreach (var node in GetTree().GetNodesInGroup("food"))
        {
            if (node is not FoodPellet pellet || !IsInstanceValid(pellet))
            {
                continue;
            }

            var distance = GlobalPosition.DistanceSquaredTo(pellet.GlobalPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = pellet;
            }
        }

        return best;
    }

    private void PickNewTarget()
    {
        var size = GetViewportRect().Size;
        if (size == Vector2.Zero)
        {
            size = new Vector2(1280, 720);
        }

        const float margin = 72.0f;
        _wanderTarget = new Vector2(
            SafeRandfRange(margin, size.X - margin),
            SafeRandfRange(margin, size.Y - margin - 96.0f));
        _reTargetTime = _random.RandfRange(1.1f, 3.7f);
        _turnSpeed = _random.RandfRange(2.1f, 4.4f);
    }

    private void KeepInsideViewport()
    {
        var size = GetViewportRect().Size;
        if (size == Vector2.Zero)
        {
            return;
        }

        const float edge = 42.0f;
        var maxX = size.X - edge;
        var maxY = size.Y - 96.0f;

        if (Position.X < edge || Position.X > maxX)
        {
            Position = new Vector2(SafeClamp(Position.X, edge, maxX), Position.Y);
            _velocity.X *= -0.72f;
            PickNewTarget();
        }

        if (Position.Y < edge || Position.Y > maxY)
        {
            Position = new Vector2(Position.X, SafeClamp(Position.Y, edge, maxY));
            _velocity.Y *= -0.72f;
            PickNewTarget();
        }
    }

    private float SafeRandfRange(float from, float to)
    {
        if (from <= to)
        {
            return _random.RandfRange(from, to);
        }

        return (from + to) * 0.5f;
    }

    private static float SafeClamp(float value, float min, float max)
    {
        if (min <= max)
        {
            return Mathf.Clamp(value, min, max);
        }

        return (min + max) * 0.5f;
    }

    private static Vector2[] EllipsePoints(Vector2 center, float radiusX, float radiusY, int count)
    {
        var points = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var angle = Mathf.Tau * i / count;
            points[i] = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
        }

        return points;
    }

    private static Color[] Fill(int count, Color color)
    {
        var colors = new Color[count];
        Array.Fill(colors, color);
        return colors;
    }
}

public partial class FoodPellet : Node2D
{
    private float _fallSpeed = 76.0f;
    private float _spin;

    public override void _Ready()
    {
        AddToGroup("food");
        ZIndex = 4;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _spin += dt * 5.0f;
        Position += new Vector2(Mathf.Sin(_spin) * 4.0f, _fallSpeed) * dt;

        var size = GetViewportRect().Size;
        if (size != Vector2.Zero && Position.Y > size.Y - 86.0f)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 5.0f, new Color(0.97f, 0.53f, 0.18f));
        DrawCircle(new Vector2(-1.6f, -1.5f), 1.5f, new Color(1.0f, 0.88f, 0.47f, 0.85f));
    }
}

public partial class Bubble : Node2D
{
    private float _radius = 8.0f;
    private float _riseSpeed = 48.0f;
    private float _drift;
    private float _phase;

    public void Configure(float radius, float riseSpeed, float drift, float phase)
    {
        _radius = radius;
        _riseSpeed = riseSpeed;
        _drift = drift;
        _phase = phase * Mathf.Tau;
    }

    public override void _Ready()
    {
        ZIndex = 20;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _phase += dt;
        Position += new Vector2(Mathf.Sin(_phase * 1.8f) * _drift, -_riseSpeed) * dt;

        if (Position.Y < -_radius * 3.0f)
        {
            QueueFree();
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var color = new Color(0.78f, 0.95f, 1.0f, 0.28f);
        DrawArc(Vector2.Zero, _radius, 0.0f, Mathf.Tau, 24, color, 1.8f);
        DrawArc(new Vector2(-_radius * 0.22f, -_radius * 0.18f), _radius * 0.36f, 3.7f, 5.4f, 8, new Color(1.0f, 1.0f, 1.0f, 0.38f), 1.4f);
    }
}
