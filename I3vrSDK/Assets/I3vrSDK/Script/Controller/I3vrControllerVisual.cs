using UnityEngine;
using i3vr;

/// Provides visual feedback for the daydream controller.
[RequireComponent(typeof(Renderer))]
public class I3vrControllerVisual:MonoBehaviour {
    [SerializeField]
    private bool isRightController;
    private I3vrController controller;

    [SerializeField]
    private Color touchPadColor =
        new Color(200f / 255f,200f / 255f,200f / 255f,1);
    [SerializeField]
    private Color appButtonColor =
        new Color(200f / 255f,200f / 255f,200f / 255f,1);
    [SerializeField]
    private Color returnButtonColor =
        new Color(200f / 255f,200f / 255f,200f / 255f,1);
    [SerializeField]
    private Color homeButtonColor =
        new Color(20f / 255f,20f / 255f,20f / 255f,1);
    

    public Color TouchPadColor
    {
        get
        {
            return touchPadColor;
        }
        set
        {
            touchPadColor = value;
            if(materialPropertyBlock != null)
            {
                materialPropertyBlock.SetColor(touchPadId,touchPadColor);
            }
        }
    }

    public Color AppButtonColor
    {
        get
        {
            return appButtonColor;
        }
        set
        {
            appButtonColor = value;
            if(materialPropertyBlock != null)
            {
                materialPropertyBlock.SetColor(appButtonId,appButtonColor);
            }
        }
    }

    public Color HomeButtonColor
    {
        get
        {
            return homeButtonColor;
        }
        set
        {
            homeButtonColor = value;
            if(materialPropertyBlock != null)
            {
                materialPropertyBlock.SetColor(homeButtonId,homeButtonColor);
            }
        }
    }

    public Color ReturnButtonColor
    {
        get
        {
            return returnButtonColor;
        }
        set
        {
            returnButtonColor = value;
            if(materialPropertyBlock != null)
            {
                materialPropertyBlock.SetColor(returnButtonId,returnButtonColor);
            }
        }
    }

    private Renderer controllerRenderer;
    private MaterialPropertyBlock materialPropertyBlock;

    private int alphaId;
    private int touchId;
    private int touchPadId;
    private int appButtonId;
    private int returnButtonId;
    private int homeButtonId;

    private bool wasTouching;
    private float touchTime;

    // Data passed to shader, (xy) touch position, (z) touch duration.
    private Vector4 controllerShaderData;
    // Data passed to shader, (y) return button duration,
    //  (z) app button click duration, (w) system button click duration.
    private Vector4 controllerShaderData2;

    // These values control animation times for the controller buttons
    public const float BUTTON_ACTIVE_DURATION_SECONDS = 0.111f;
    public const float BUTTON_RELEASE_DURATION_SECONDS = 0.0909f;

    public const float TOUCHPAD_POINT_SCALE_DURATION_SECONDS = 0.15f;

    // How much time to use as an 'immediate update'.
    // Any value large enough to instantly update all visual animations.
    private const float IMMEDIATE_UPDATE_TIME = 10f;

    private void Start()
    {
        Initialize();
        controller = I3vrControllerManager.RightController;
        if(!isRightController)
        {
            controller = I3vrControllerManager.LeftController;
        }
    }

    private void Initialize()
    {
        if(controllerRenderer == null)
        {
            controllerRenderer = GetComponent<Renderer>();
        }
        if(materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        alphaId = Shader.PropertyToID("_I3vrControllerAlpha");
        touchId = Shader.PropertyToID("_I3vrTouchInfo");
        touchPadId = Shader.PropertyToID("_I3vrTouchPadColor");
        appButtonId = Shader.PropertyToID("_I3vrAppButtonColor");
        returnButtonId = Shader.PropertyToID("_I3vrReturnButtonColor");
        homeButtonId = Shader.PropertyToID("_I3vrHomeButtonColor");

        materialPropertyBlock.SetColor(appButtonId,appButtonColor);
        materialPropertyBlock.SetColor(returnButtonId,returnButtonColor);
        materialPropertyBlock.SetColor(homeButtonId,homeButtonColor);
        materialPropertyBlock.SetColor(touchPadId,touchPadColor);
        controllerRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    public void OnVisualUpdate(bool updateImmediately = false)
    {
        float deltaTime = Time.deltaTime;
        // If flagged to update immediately, set deltaTime to an arbitrarily large value
        // This is particularly useful in editor, but also for resetting state quickly
        if(updateImmediately)
        {
            deltaTime = IMMEDIATE_UPDATE_TIME;
        }

        if(controller.ReturnButton)
        {
            controllerShaderData2.y = Mathf.Min(1,controllerShaderData2.y + deltaTime / BUTTON_ACTIVE_DURATION_SECONDS);
        }
        else
        {
            controllerShaderData2.y = Mathf.Max(0,controllerShaderData2.y - deltaTime / BUTTON_RELEASE_DURATION_SECONDS);
        }

        if(controller.AppButton)
        {
            controllerShaderData2.z = Mathf.Min(1,controllerShaderData2.z + deltaTime / BUTTON_ACTIVE_DURATION_SECONDS);
        }
        else
        {
            controllerShaderData2.z = Mathf.Max(0,controllerShaderData2.z - deltaTime / BUTTON_RELEASE_DURATION_SECONDS);
        }

        if(controller.HomeButton)
        {
            controllerShaderData2.w = Mathf.Min(1,controllerShaderData2.w + deltaTime / BUTTON_ACTIVE_DURATION_SECONDS);
        }
        else
        {
            controllerShaderData2.w = Mathf.Max(0,controllerShaderData2.w - deltaTime / BUTTON_RELEASE_DURATION_SECONDS);
        }

        materialPropertyBlock.SetVector(alphaId,controllerShaderData2);

        controllerShaderData.x = -controller.TouchPosCentered.x;
        controllerShaderData.y = -controller.TouchPosCentered.y;

        if(controller.IsTouching)
        {
            if(!wasTouching)
            {
                wasTouching = true;
            }
            if(touchTime < 1)
            {
                touchTime = Mathf.Min(touchTime + deltaTime / TOUCHPAD_POINT_SCALE_DURATION_SECONDS,1);
            }
        }
        else
        {
            wasTouching = false;
            if(touchTime > 0)
            {
                touchTime = Mathf.Max(touchTime - deltaTime / TOUCHPAD_POINT_SCALE_DURATION_SECONDS,0);
            }
        }

        controllerShaderData.z = touchTime;

        materialPropertyBlock.SetVector(touchId,controllerShaderData);

        // Update the renderer
        controllerRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnEnable()
    {
        controller.OnPostControllerInputUpdated += OnPostControllerInputUpdated;
    }

    private void OnDisable()
    {
        controller.OnPostControllerInputUpdated -= OnPostControllerInputUpdated;
    }

    private void OnPostControllerInputUpdated()
    {
        OnVisualUpdate();
    }
}
