using System.Windows.Forms;
using UnityEngine;

namespace Create.MultiScreen
{
    public class MultiScreenController : MonoBehaviour
    {
        private int _connectedDisplaysAtStart;
        private bool _correctAmountOfScreensConnected;

        private void Start()
        {
            _connectedDisplaysAtStart = SystemInformation.MonitorCount;
            if (Display.displays.Length > 1)
            {
                _correctAmountOfScreensConnected = true;
                foreach (var item in Display.displays)
                {
                    item.Activate(1920,1200,60);
                }
            }
        }

        private void Update()
        {
            if (!_correctAmountOfScreensConnected)
            {
                if (SystemInformation.MonitorCount != _connectedDisplaysAtStart) //Amount of connected monitors has changed, quit application
                {
                    UnityEngine.Application.Quit();
                }
            }
        }
    }
}