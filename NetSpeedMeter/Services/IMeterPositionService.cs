using System.Windows;
using Point = System.Windows.Point;

namespace NetSpeedMeter.Services
{
    public interface IMeterPositionService
    {
        Point CalculateDefaultPosition(double windowWidth, double windowHeight);
        void ApplySavedOrCalculatedPosition(Window window);
        void SavePosition(Window window);
        void ResetToTaskbarPosition(Window window);
    }
}
