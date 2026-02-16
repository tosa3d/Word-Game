// In a file named HapticFeedback.mm
#import <UIKit/UIKit.h>

extern "C" {
    void _TriggerHapticFeedback(int force)
    {
        UIImpactFeedbackGenerator *generator;
        switch (force)
        {
            case 0:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                break;
            case 1:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                break;
            case 2:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                break;
            default:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                break;
        }
        [generator prepare];
        [generator impactOccurred];
    }
}