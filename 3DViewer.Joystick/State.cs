namespace _3DViewer.Joystick
{
    public class State
    {
        public static int CenterX { get; set; }
        public static int CenterY { get; set; }
        public static int centerDelta = 100;
        public static int bounceDelta = 70;
        public int X { get; set; }
        public int Y { get; set; }

        public List<string> pressedButtons = new List<string>();
        public State()
        {
            CenterX = -1;
            CenterY = -1;
        }
        public void RereadState(string readString)
        {
            string[] res = readString.Split(";");
            int temp;
            pressedButtons.Clear();
            foreach (string s in res)
            {
                string[] attr = s.Split(":");
                switch (attr[0])
                {
                    case "BTN":
                        pressedButtons.Add(attr[1]);
                        break;
                    case "X":
                        if (Int32.TryParse(attr[1], out temp))
                        {
                            X = temp;
                            if(CenterX < 0)
                            {
                                CenterX = temp;
                            }
                        }
                        break;
                    case "Y":
                        if (Int32.TryParse(attr[1], out temp))
                        {
                            Y = temp;
                            if (CenterY < 0)
                            {
                                CenterY = temp;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        public void CopyToState(ref State state)
        {
            if(CenterX < 0)
            {
                CenterX = X;
                CenterY = Y;
            }
            state.X = X;
            state.Y = Y;
            state.pressedButtons.Clear();
            foreach (var button in pressedButtons)
            {
                state.pressedButtons.Add(button);
            }
        }
    }
}