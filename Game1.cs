using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
using Color = Microsoft.Xna.Framework.Color;
using System.Linq;

namespace MonoCirclesMove
{
    public class Game1 : Game
    {
        // Graphics and Rendering
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font; // 字体用于绘制文本

        // Textures
        private Texture2D circleTexture;
        private Texture2D circleHighlightTexture;
        private Texture2D whitePixel;

        // Game State
        private DynamicArray<Circle> circles;
        private Circle followedCircle = null;  // 当前正在跟随的圆形
        private Circle previousFollowedCircle = null;
        private Map map;

        // User Input
        private MouseState previousMouseState;
        private KeyboardState previousKeyboardState;
        private float scale_mouse = 1.0f;
        private bool isFollowing = false;  // 是否正在跟随

        // Camera
        private Vector2 cameraPosition;
        private Vector2 targetPosition;
        private float targetScale;

        // Performance Measurements
        private int totalFrames = 0;
        private float elapsedTime = 0.0f;
        private int fps = 0;
        private Queue<float> recentFrameTimes = new Queue<float>();

        // Misc
        private Random random;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            _graphics.PreferredBackBufferWidth = 1400;  // 设置窗体的宽度
            _graphics.PreferredBackBufferHeight = 900; // 设置窗体的高度
            _graphics.PreferMultiSampling = true; // 启用 MSAA
            Window.Title = "图形渲染压力测试By无知的克鲁  WSAD-移动视角 鼠标滚轮-缩放大小 V键-随机小球视野跟随";  // 设置窗口标题
            IsMouseVisible = true;
            random = new Random();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            // Initialize circles with random positions and sizes
            previousMouseState = Mouse.GetState();
            map = new Map(150000, 150000);//初始化地图参数
            targetScale = scale_mouse; // 初始时，目标缩放值和当前的缩放值相同
            cameraPosition = new Vector2(map.Bounds.Width* 0.5f, map.Bounds.Height * 0.5f);
            targetPosition = cameraPosition;
            circles = new DynamicArray<Circle>(200000);
            for (int i = 0; i < 200000; i++)
            {
                Vector2 position = new Vector2(random.Next(_graphics.PreferredBackBufferWidth), random.Next(_graphics.PreferredBackBufferHeight));
                do
                    position = new Vector2(random.Next(map.Bounds.Width), random.Next(map.Bounds.Height));
                while (!map.ContainsPoint(position));

                Vector2 velocity = new Vector2(((float)random.NextDouble() - 0.5f) * 1.3f , ((float)random.NextDouble() - 0.5f) * 1.3f);
                //velocity.Normalize();
                circles.Add(new Circle(position, velocity, random.Next(40, 105)));
            }


            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a simple circle texture.
            circleTexture = CreateCircleTexture(100, Color.White);

            // 创建一个只有边界的圆形纹理
            circleHighlightTexture = CreateCircleBorderTexture(100, Color.White, 15);


            // 导入字体纹理
            _font = Content.Load<SpriteFont>("Standard");

            //  创建一个1x1大小的白色纹理
            whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });

        }

        protected override void Update(GameTime gameTime)
        {
            // Input handling
            var keyboardState = Keyboard.GetState();
            var currentMouseState = Mouse.GetState();
            var zoomSpeed = 0.25f; // Adjust as needed
            var justPressedV = false;

            float frameTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            recentFrameTimes.Enqueue(frameTime);

            while (recentFrameTimes.Count > 60)  // 只保留最近的20帧
                recentFrameTimes.Dequeue();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (!isFollowing)  // 如果不在跟随模式下，允许手动移动视野
            {
                const float cameraSpeed = 600f;
                if (keyboardState.IsKeyDown(Keys.W))
                    targetPosition.Y -= cameraSpeed;
                if (keyboardState.IsKeyDown(Keys.S))
                    targetPosition.Y += cameraSpeed;
                if (keyboardState.IsKeyDown(Keys.A))
                    targetPosition.X -= cameraSpeed;
                if (keyboardState.IsKeyDown(Keys.D))
                    targetPosition.X += cameraSpeed;
            }

            if (keyboardState.IsKeyDown(Keys.V) && !previousKeyboardState.IsKeyDown(Keys.V))  // 检查是否刚按下 V 键
            {
                isFollowing = !isFollowing;  // 切换跟随状态
                justPressedV = true;
                if (isFollowing)
                {
                    do
                    {
                        int index = random.Next(circles.Count);  // 随机选择一个圆形
                        followedCircle = circles[index];
                    }
                    while (followedCircle == previousFollowedCircle);  // 如果选中的圆形和上次跟随的圆形相同，那么重新选择
                    targetPosition = followedCircle.Position;
                }
                else  // 如果停止跟随，将视野移动到屏幕中心
                {
                    targetPosition = cameraPosition;
                }
            }

            if (isFollowing && (followedCircle == null || justPressedV))  // 如果正在跟随且没有正在跟随的圆形，或者刚刚按下了 V 键
            {
                do
                {
                    int index = random.Next(circles.Count);  // 随机选择一个圆形
                    followedCircle = circles[index];
                }
                while (followedCircle == previousFollowedCircle);  // 如果选中的圆形和上次跟随的圆形相同，那么重新选择
                targetPosition = followedCircle.Position;
            }

            if (isFollowing)
            {
                targetPosition = followedCircle.Position;
            }


            // Update the scale based on the scroll wheel change
            // 这里使用加性的缩放逻辑
            if (currentMouseState.ScrollWheelValue > previousMouseState.ScrollWheelValue)
            {
                // Scroll wheel moved up, increase target scale
                targetScale *= 1 + zoomSpeed;
            }
            else if (currentMouseState.ScrollWheelValue < previousMouseState.ScrollWheelValue)
            {
                // Scroll wheel moved down, decrease target scale
                targetScale *= 1 - zoomSpeed;
            }

            // Smoothly transition scale_mouse to targetScale
            scale_mouse += (targetScale - scale_mouse) * 0.1f;

            // Smoothly transition scale_mouse to targetScale
            cameraPosition += (targetPosition - cameraPosition) * 0.9f;

            justPressedV = false;

            previousMouseState = currentMouseState;
            previousKeyboardState = keyboardState;

            // Update circle positions
            for (int i = 0; i < circles.Count; i++)
            {
                // update acceleration
                if (random.NextDouble() < 0.02)
                { // 2% chance to change acceleration
                    circles[i].Acceleration = new Vector2((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                }

                circles[i].Velocity += circles[i].Acceleration ;
                circles[i].Position += circles[i].Velocity;

                if (!map.ContainsPoint(circles[i].Position))
                {
                    circles[i].Velocity = -circles[i].Velocity;
                }

                // Limit velocity to max velocity
                if (circles[i].Velocity.Length() > circles[i].MaxVelocity)
                {
                    circles[i].Velocity = Vector2.Normalize(circles[i].Velocity) * circles[i].MaxVelocity;
                }

            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            // Apply camera transform
            Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);

            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            _spriteBatch.Begin();

            ////fill bounder
            //Vector2 mapCenter = new Vector2(map.Bounds.Center.X, map.Bounds.Center.Y);
            //Vector2 relativeMapCenter;
            //if (isFollowing)
            //{
            //    relativeMapCenter = mapCenter - followedCircle.Position;
            //}
            //else
            //{
            //    relativeMapCenter = mapCenter - cameraPosition;
            //}
            //Vector2 screenRelativeMapCenter = (relativeMapCenter * scale_mouse) + screenCenter;
            //Vector2 drawMapPosition = screenRelativeMapCenter - new Vector2(map.Bounds.Width * scale_mouse / 2f, map.Bounds.Height * scale_mouse / 2f);
            //Vector2 scaleMap = new Vector2(map.Bounds.Width * scale_mouse, map.Bounds.Height * scale_mouse);
            //_spriteBatch.Draw(whitePixel, drawMapPosition, null, Color.White, 0f, Vector2.Zero, scaleMap, SpriteEffects.None, 0f);

            // Draw map border
            Vector2 topLeft = new Vector2(map.Bounds.Left, map.Bounds.Top);
            Vector2 topRight = new Vector2(map.Bounds.Right, map.Bounds.Top);
            Vector2 bottomLeft = new Vector2(map.Bounds.Left, map.Bounds.Bottom);
            Vector2 bottomRight = new Vector2(map.Bounds.Right, map.Bounds.Bottom);

            // Convert map corners to screen space
            topLeft = (topLeft - cameraPosition) * scale_mouse + screenCenter;
            topRight = (topRight - cameraPosition) * scale_mouse + screenCenter;
            bottomLeft = (bottomLeft - cameraPosition) * scale_mouse + screenCenter;
            bottomRight = (bottomRight - cameraPosition) * scale_mouse + screenCenter;

            float borderWidth = 4f; // Adjust as needed
            Color borderColor = Color.White; // Adjust as needed

            // Draw border lines
            DrawLine(_spriteBatch, whitePixel, topLeft, topRight, borderColor, borderWidth);
            DrawLine(_spriteBatch, whitePixel, topRight, bottomRight, borderColor, borderWidth);
            DrawLine(_spriteBatch, whitePixel, bottomRight, bottomLeft, borderColor, borderWidth);
            DrawLine(_spriteBatch, whitePixel, bottomLeft, topLeft, borderColor, borderWidth);

            // Draw circle
            foreach (var circle in circles)
            {
                Vector2 origin = new Vector2(circleTexture.Width / 2f, circleTexture.Height / 2f);
                // 计算相对于视野（或被跟踪的圆）的位置
                Vector2 relativePosition;
                if (isFollowing)
                {
                    relativePosition = circle.Position - followedCircle.Position;
                }
                else
                {
                    relativePosition = circle.Position - cameraPosition;
                }

                // 计算相对于屏幕中心的位置，并考虑缩放
                Vector2 screenRelativePosition = (relativePosition * scale_mouse) + screenCenter;

                // 计算出缩放后的半径
                float scaledRadius = circle.Radius * scale_mouse;

                // 计算出绘制位置和缩放
                Vector2 drawPosition = screenRelativePosition - new Vector2(scaledRadius);
                Vector2 scale = new Vector2(scaledRadius * 2) / new Vector2(circleTexture.Width, circleTexture.Height);

                //渲染填充圆
                _spriteBatch.Draw(circleTexture, screenRelativePosition, null, circle.Color, 0f, origin, scale, SpriteEffects.None, 0f);

                //如果正在跟随这个圆，那么绘制一个描绘的圆作为高亮
                if (isFollowing && circle == followedCircle)
                {
                    // 计算出高亮圆的尺寸
                    Vector2 highlightScale = scale * 1.5f;  // 高亮圆的尺寸稍大于原圆

                    // 计算出绘制位置和缩放
                    Vector2 drawHighlightPosition = screenRelativePosition - new Vector2(circleHighlightTexture.Width * highlightScale.X / 2);
                    Vector2 scaleHighlight = new Vector2(circleHighlightTexture.Width * highlightScale.X, circleHighlightTexture.Height * highlightScale.Y) / new Vector2(circleHighlightTexture.Width, circleHighlightTexture.Height);

                    // 绘制高亮圆
                    _spriteBatch.Draw(circleHighlightTexture, drawHighlightPosition, null, Color.Purple, 0f, Vector2.Zero, scaleHighlight, SpriteEffects.None, 0f);
                }
            }

            // Frame rate counter logic
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            totalFrames++;

            if (elapsedTime >= 1.0f)
            {
                fps = totalFrames;
                totalFrames = 0;
                elapsedTime = 0;
            }

            // Draw FPS
            _spriteBatch.DrawString(_font, "FPS: " + fps.ToString(), new Vector2(10, 0), Color.White);

            //// 获取当前进程
            //var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            //// 获取内存使用（单位：字节）
            //long memoryUsage = currentProcess.WorkingSet64;

            //// 获取CPU使用时间（单位：时间间隔）
            //TimeSpan cpuTime = currentProcess.TotalProcessorTime;

            //_spriteBatch.DrawString(_font, "Memory Usage: " + (memoryUsage / 1024 / 1024).ToString() + " MB", new Vector2(10, 20), Color.White);
            //_spriteBatch.DrawString(_font, "CPU Time: " + cpuTime.TotalSeconds.ToString() + " s", new Vector2(10, 40), Color.White);

            //Frame Time Stability帧时间稳定性
            if (recentFrameTimes.Count >= 2)
            {
                float mean = recentFrameTimes.Average();
                float sum = (float)recentFrameTimes.Sum(d => Math.Pow(d - mean, 2));
                float standardDeviation = (float)Math.Sqrt((1.0 / recentFrameTimes.Count) * sum);
                _spriteBatch.DrawString(_font, "FTS: " + standardDeviation.ToString(), new Vector2(10, 20), Color.White);
            }

            // Draw circle count
            _spriteBatch.DrawString(_font, "Circles: " + circles.Count.ToString(), new Vector2(10, 40), Color.White);

            // 是否跟随小球
            _spriteBatch.DrawString(_font, "跟随模式是否启动: " + isFollowing, new Vector2(10, 60), Color.White);


            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private Texture2D CreateCircleTexture(int radius, Color color)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];
            float radiusSquared = radius * radius;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int index = x + y * diameter;
                    Vector2 position = new Vector2(x - radius, y - radius);
                    if (position.LengthSquared() <= radiusSquared)
                    {
                        colorData[index] = color;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        
    }

        private Texture2D CreateCircleBorderTexture(int radius, Color color, int borderThickness)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];
            float radiusSquared = radius * radius;
            float borderSquared = (radius - borderThickness) * (radius - borderThickness);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int index = x + y * diameter;
                    Vector2 position = new Vector2(x - radius, y - radius);
                    float lengthSquared = position.LengthSquared();

                    if (lengthSquared <= radiusSquared && lengthSquared >= borderSquared)
                    {
                        colorData[index] = color;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        public void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
        {
            var length = Vector2.Distance(start, end);
            var rotation = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            spriteBatch.Draw(texture, start, null, color, rotation, Vector2.Zero, new Vector2(length, width), SpriteEffects.None, 0f);
        }

    }

    public class Circle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Acceleration { get; set; }
        public float Radius { get; set; }
        public float MaxVelocity { get; set; }
        public Color Color { get; set; }
        private static Random random = new Random();

        public Circle(Vector2 position, Vector2 velocity, float radius)
        {
            Position = position;
            Velocity = velocity * 0.5f; // Reduce initial velocity
            Radius = radius;
            MaxVelocity = (float)random.NextDouble() * 5.0f + 20f; // Set a random max velocity between 20 and 25
            Color = new Color(random.Next(256), random.Next(256), random.Next(256));
            Acceleration = new Vector2((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5)) * 0.1f; // Reduce acceleration
        }
    }

    public class Map
    {
        public Rectangle Bounds { get; private set; }

        public Map(int width, int height)
        {
            Bounds = new Rectangle(0, 0, width, height);
        }

        public bool ContainsPoint(Vector2 point)
        {
            return Bounds.Contains(point);
        }
    }
}