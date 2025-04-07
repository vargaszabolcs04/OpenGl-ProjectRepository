using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static IWindow window;
        private static GL Gl;
        private static uint program;
        private static List<(GlCube cube, Vector3D<float> position)> rubikCubes = new();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 vPos;
            layout (location = 1) in vec4 vCol;

            uniform mat4 uModel;
            uniform mat4 uView;
            uniform mat4 uProjection;

            out vec4 outCol;
            
            void main()
            {
                outCol = vCol;
                gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
            }
        ";

        private static readonly string FragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;
            in vec4 outCol;

            void main()
            {
                FragColor = outCol;
            }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Rubik's Cube";
            windowOptions.Size = new Vector2D<int>(800, 600);
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);
            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;
            window.Run();
        }

        private static void Window_Load()
        {
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            for (int i = 0; i < 9; i++) cameraDescriptor.IncreaseZXAngle();
            for (int i = 0; i < 6; i++) cameraDescriptor.IncreaseZYAngle();

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);
            SetUpObjects();
            LinkProgram();
            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader error: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
                throw new Exception("Shader link error: " + Gl.GetProgramInfoLog(program));

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left: cameraDescriptor.DecreaseZYAngle(); break;
                case Key.Right: cameraDescriptor.IncreaseZYAngle(); break;
                case Key.Down: cameraDescriptor.IncreaseDistance(); break;
                case Key.Up: cameraDescriptor.DecreaseDistance(); break;
                case Key.U: cameraDescriptor.IncreaseZXAngle(); break;
                case Key.D: cameraDescriptor.DecreaseZXAngle(); break;
            }
        }

        private static void Window_Update(double deltaTime) { }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            foreach (var (cube, position) in rubikCubes)
            {
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateTranslation(position * 1.1f);
                SetModelMatrix(modelMatrix);

                Gl.BindVertexArray(cube.Vao);
                Gl.DrawElements(GLEnum.Triangles, cube.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }
        }

        private static void SetUpObjects()
        {
            rubikCubes.Clear();

            for (float x = -1; x <= 1; x += 1)
            {
                for (float y = -1; y <= 1; y += 1)
                {
                    for (float z = -1; z <= 1; z += 1)
                    {
                        Vector3D<float> position = new(x, y, z);

                        float[] face1Color = GetFaceColor(position, 0); // Felső
                        float[] face2Color = GetFaceColor(position, 1); // Első
                        float[] face3Color = GetFaceColor(position, 2); // Bal
                        float[] face4Color = GetFaceColor(position, 3); // Alsó
                        float[] face5Color = GetFaceColor(position, 4); // Hátsó
                        float[] face6Color = GetFaceColor(position, 5); // Jobb

                        GlCube cube = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
                        rubikCubes.Add((cube, position));
                    }
                }
            }
        }

        private static float[] GetFaceColor(Vector3D<float> position, int faceIndex)
        {
            float[] grey = { 0.3f, 0.3f, 0.3f, 1f };
            bool isOuter = false;

            switch (faceIndex)
            {
                case 0: isOuter = position.Y == 1; break;  // Felső (Fehér)
                case 1: isOuter = position.Z == 1; break;  // Első (Piros)
                case 2: isOuter = position.X == -1; break; // Bal (Kék)
                case 3: isOuter = position.Y == -1; break; // Alsó (Sárga)
                case 4: isOuter = position.Z == -1; break; // Hátsó (Narancs)
                case 5: isOuter = position.X == 1; break;  // Jobb (Zöld)
            }

            if (!isOuter) return grey;

            // Rubik-kocka eredeti színei
            switch (faceIndex)
            {
                case 0: return new float[] { 1.0f, 1.0f, 1.0f, 1.0f }; // Fehér
                case 1: return new float[] { 1.0f, 0.0f, 0.0f, 1.0f }; // Piros
                case 2: return new float[] { 0.0f, 0.0f, 1.0f, 1.0f }; // Kék
                case 3: return new float[] { 1.0f, 1.0f, 0.0f, 1.0f }; // Sárga
                case 4: return new float[] { 1.0f, 0.5f, 0.0f, 1.0f }; // Narancs
                case 5: return new float[] { 0.0f, 1.0f, 0.0f, 1.0f }; // Zöld
                default: return grey;
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 800f / 600f, 0.1f, 100f);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
        }

        private static void Window_Closing()
        {
            foreach (var (cube, _) in rubikCubes)
            {
                cube.ReleaseGlCube();
            }
        }
    }
}