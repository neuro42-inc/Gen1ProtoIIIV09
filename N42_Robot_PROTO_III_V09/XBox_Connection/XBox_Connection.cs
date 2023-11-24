using SharpDX.XInput;

namespace n42_Robot_PROTO_III
{
    public class XBox_XInputController
    {
        Controller XBox_controller;
        public bool XBox_connected = false;

        public XBox_XInputController()
        {
            XBox_controller = new Controller(UserIndex.One);
            XBox_connected = XBox_controller.IsConnected;
        }
    }

}
