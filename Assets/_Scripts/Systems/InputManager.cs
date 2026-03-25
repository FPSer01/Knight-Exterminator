using UnityEngine;

public class InputManager
{
    private static InputActions input;

    public static InputActions Input
    {
        get
        {
            if (input == null)
            {
                input = new InputActions();
                input.Enable();
            }

            return input;
        }
    }
}
