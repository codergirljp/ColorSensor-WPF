namespace jp.Comms
{
    public class ColorSensorProtocol
    {
        public int R = 0;
        public int G = 0;
        public int B = 0;
        public string HexColor = "000000";
        public bool LightIsOn = true;
        public int ClearK = 0;
        public int RedK = 0;
        public int GreenK = 0;
        public int BlueK = 0;

        public void GetData(string message)
        {
            string[] msgSplit = message.Split(' ');
            if (msgSplit[0] == "RGB:")
            {
                R = System.Convert.ToInt32(msgSplit[1]);
                G = System.Convert.ToInt32(msgSplit[2]);
                B = System.Convert.ToInt32(msgSplit[3]);
                HexColor = msgSplit[4].ToString();
                LightIsOn = System.Convert.ToBoolean(System.Convert.ToInt32(msgSplit[5]));
                ClearK = System.Convert.ToInt32(msgSplit[6]);
                RedK = System.Convert.ToInt32(msgSplit[7]);
                GreenK = System.Convert.ToInt32(msgSplit[8]);
                BlueK = System.Convert.ToInt32(msgSplit[9]);
            }
        }
    }
}

