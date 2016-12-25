using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class XKShadow : MonoBehaviour 
{
    [SerializeField]
    private Color shadowColor = Color.gray;

    [SerializeField]
	private LayerMask shadowLayers = -2;

    [SerializeField]
    [Range(2, 12)]
	private float shadowArea = 2;

	[SerializeField]
    [Range(1, 4)]
	private int shadowResolution = 1;

    [SerializeField]
    [Range(5, 20)]
	private float lightSourceNear = 5.0f;

    [SerializeField]
    [Range(10, 50)]
	private float lightSourceFar = 18.0f;

	[SerializeField]
	private Vector2 centerOffset = Vector2.zero;

	[SerializeField]
	private string followingTargetTag = null;

	[SerializeField]
	[Range(0, 10)]
	private int blurTimes = 0;

	[SerializeField]
	[Range(0, 3)]
	private int blurDownSampleLevel = 0;

	[SerializeField]
	[Range(-0.5f, 0.5f)]
	private float shadowExpansion = 0.0f;

	[SerializeField]
	private bool isFadeOn = false;

	[SerializeField]
	private Vector2 fadeWorldY = new Vector2(0, 100);

	[SerializeField]
	private bool debugRT = false;

    [SerializeField]
    [HideInInspector]
	private Vector3 lightDir = -Vector3.one;

	private Shader xkShadowCaster = null;

	private Shader xkShadowFadeCaster = null;

	private Material blurMtrl = null;

    private Transform sourcePoint = null;

    private Transform thisTransform = null;

    private Camera shadowCamera = null;

    private Transform shadowCameraTransform = null;

    private RenderTexture shadowRT = null;

    private Transform followingTarget = null;

    private int updateShadowCameraFlag = 0;

    private float shadowRTMargin = 0.01f;

    private int shadowColorPropId = 0;

    private int shadowRTMarginPropId = 0;

    private int shadowRTPropId = 0;

    private int shadowVPPropId = 0;

	private int shadowExpansionPropId = 0;

    private int shadowFarClipPlanePropId = 0;

    private int shadowCamViewMatPropId = 0;

	private int fadeWorldYPropId = 0;

    private GameObject clearCamGo = null;

    private Camera clearCam = null;

    // EncodeFloatRG(0.99999f)
	private Color clearColor = new Color(0.9960784f, 0.9974518f, 1, 0);

	public void ResetFollowingTarget(string tag)
	{
		followingTargetTag = tag;
		followingTarget = null;
	}

    private void Awake()
    {
		InitShaderPropertyID();

        thisTransform = transform;

        Shader.SetGlobalTexture(shadowRTPropId, Texture2D.whiteTexture);
    }

    private void Start()
    {
        Camera mainCam = GetComponent<Camera>();
        if (mainCam == null || mainCam != Camera.main)
        {
            Debug.LogError("将脚本挂载到主相机上");
            DestroyObj(this);
            return;
        }

        if(shadowLayers == -2)
        {
            shadowLayers = LayerMask.GetMask("Default");
        }

#if UNITY_EDITOR
        CreateSourcePoint();
#endif
		InitShaderAndMaterial();
        CreateCamera();
    }

	private void InitShaderPropertyID()
	{
		shadowColorPropId = Shader.PropertyToID("_XKShadowColor");
		shadowRTMarginPropId = Shader.PropertyToID("_XKShadowRTMargin");
		shadowRTPropId = Shader.PropertyToID("_XKShadowRT");
		shadowVPPropId = Shader.PropertyToID("_XKShadowVP");
		shadowFarClipPlanePropId = Shader.PropertyToID("_XKShadowFarClipPlane");
		shadowCamViewMatPropId = Shader.PropertyToID("_XKShadowCamViewMat");
		shadowExpansionPropId = Shader.PropertyToID("_XKShadowExpansion");
		fadeWorldYPropId = Shader.PropertyToID("_XKShadowFadeWorldY");
	}

	private void InitShaderAndMaterial()
	{
		xkShadowCaster = Shader.Find("Hidden/XKShadow/XKShadowCaster");
		xkShadowFadeCaster = Shader.Find("Hidden/XKShadow/XKShadowFadeCaster");
		blurMtrl = new Material(Shader.Find("Hidden/XKShadow/XKShadowBlur"));
	}

	private void DestroyShaderAndMaterial()
	{
		xkShadowCaster = null;
		xkShadowFadeCaster = null;

		if(blurMtrl != null)
		{
			DestroyObj(blurMtrl);
			blurMtrl = null;
		}
	}

	private void CheckFadeWorldY()
	{
		fadeWorldY.x = Mathf.Min(fadeWorldY.x, fadeWorldY.y);
	}

    private void CreateSourcePoint()
    {
        if (sourcePoint == null)
        {
            GameObject go = new GameObject("XKShadowSourcePoint");
            go.hideFlags = HideFlags.HideAndDontSave;
            sourcePoint = go.transform;
            sourcePoint.parent = null;
            sourcePoint.localScale = Vector3.one;
            sourcePoint.position = thisTransform.position;
            sourcePoint.forward = lightDir;
        }
    }

    private void CreateCamera()
    {
        GameObject go = new GameObject("XKShadowCamera");
        go.hideFlags = HideFlags.HideAndDontSave;
        shadowCameraTransform = go.transform;
        shadowCameraTransform.parent = null;
        shadowCamera = go.AddComponent<Camera>();
        shadowCamera.orthographic = true;
        shadowCamera.orthographicSize = shadowArea;
        shadowCamera.cullingMask = shadowLayers;
        shadowCamera.depth = -100;
        shadowCamera.backgroundColor = clearColor;
        shadowCamera.enabled = false;
        shadowCamera.farClipPlane = 150;
        shadowCamera.rect = new Rect(shadowRTMargin, shadowRTMargin, 1 - shadowRTMargin * 2, 1 - shadowRTMargin * 2);

        CreateCameraRT();
    }

    private void DestroyCameraRT()
    {
        if (shadowRT != null)
        {
			DestroyObj(shadowRT);
            shadowRT = null;
        } 
    }

    private void CreateCameraRT()
    {
        if(shadowCamera == null)
        {
            return;
        }

        int size = ConvertShadowResolutionToSize();
        if (shadowRT == null)
        {
            shadowRT = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            shadowRT.hideFlags = HideFlags.HideAndDontSave;
        }
        else
        {
            shadowRT.Release();
            shadowRT.width = size;
            shadowRT.height = size;
        }

        if (clearCamGo == null)
        {
            clearCamGo = new GameObject("XKShadowClearCamera");
            clearCamGo.hideFlags = HideFlags.HideAndDontSave;
            clearCam = clearCamGo.AddComponent<Camera>();
            clearCam.clearFlags = CameraClearFlags.SolidColor;
            clearCam.backgroundColor = clearColor;
            clearCam.enabled = false;
            clearCam.cullingMask = 0;
        }

        shadowRT.wrapMode = TextureWrapMode.Clamp;
        shadowRT.Create();
        shadowCamera.targetTexture = null;
        clearCam.targetTexture = shadowRT;
        clearCam.Render();
        clearCam.targetTexture = null;
        shadowCamera.targetTexture = shadowRT;
    }

    private int ConvertShadowResolutionToSize()
    {
        return shadowResolution == 1 ? 256 :
                shadowResolution == 2 ? 512 :
                shadowResolution == 3 ? 1024 : 
                shadowResolution == 4 ? 2048 : 512;
    }

    private void CheckCameraRT()
    {
        if (shadowRT != null && !shadowRT.IsCreated())
        {
            CreateCameraRT();
        }
    }

    private void UpdateShadowCamera()
    {
        lightDir.Normalize();

        if (Application.isPlaying)
        {
            if (updateShadowCameraFlag == Time.frameCount)
            {
                return;
            }
            updateShadowCameraFlag = Time.frameCount;
        }

        if(followingTarget == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(followingTargetTag);
            if(go != null)
            {
                followingTarget = go.transform;
            }
        }
        if(followingTarget != null && shadowCameraTransform != null && shadowCamera != null)
        {
            Vector3 playerPos = followingTarget.position;
            Vector3 pos = playerPos - lightDir * (lightSourceNear + shadowCamera.farClipPlane * 0.5f);
            pos.x += centerOffset.x;
            pos.z += centerOffset.y;
            shadowCameraTransform.position = pos;
            shadowCameraTransform.forward = lightDir;
        }

        if (shadowCamera != null)
        {
            CheckCameraRT();
			CheckFadeWorldY();

            Shader.SetGlobalColor(shadowColorPropId, shadowColor);
            Shader.SetGlobalFloat(shadowRTMarginPropId, shadowRTMargin);
            Shader.SetGlobalTexture(shadowRTPropId, shadowRT);
            Shader.SetGlobalMatrix(shadowVPPropId, shadowCamera.projectionMatrix * shadowCamera.worldToCameraMatrix);
            Shader.SetGlobalFloat(shadowFarClipPlanePropId, shadowCamera.farClipPlane);
            Shader.SetGlobalMatrix(shadowCamViewMatPropId, shadowCamera.worldToCameraMatrix);
			Shader.SetGlobalFloat(shadowExpansionPropId, shadowExpansion);
			Shader.SetGlobalVector(fadeWorldYPropId, fadeWorldY);

            if(shadowLayers != shadowCamera.cullingMask)
            {
                shadowCamera.cullingMask = shadowLayers;
            }
            UpdateShadowArea();
            if (shadowRT != null && shadowRT.width != ConvertShadowResolutionToSize())
            {
                CreateCameraRT();
            }
            if (shadowCamera != null && shadowCamera.farClipPlane != lightSourceFar)
            {
                shadowCamera.farClipPlane = lightSourceFar;
            }

			Shader xkShadowCaster = null;
			if(isFadeOn)
			{
				xkShadowCaster = this.xkShadowFadeCaster;
				shadowCamera.clearFlags = CameraClearFlags.Nothing;
				Graphics.SetRenderTarget(shadowRT);
				GL.Clear(true, true, clearColor,0.0f);
				Graphics.SetRenderTarget(null);
			}
			else
			{
				xkShadowCaster = this.xkShadowCaster;
				shadowCamera.clearFlags = CameraClearFlags.SolidColor;
				shadowCamera.backgroundColor = clearColor;
			}

			shadowCamera.RenderWithShader(xkShadowCaster, null);

			if(blurTimes > 0)
			{
				int blurRTsize = shadowRT.width;
				if(blurDownSampleLevel > 0)
				{
					for(int i = 0; i < blurDownSampleLevel; ++i)
					{
						blurRTsize /= 2;
					}
				}

				RenderTexture src = RenderTexture.GetTemporary(blurRTsize, blurRTsize, 0, RenderTextureFormat.ARGB32);
				RenderTexture dest = RenderTexture.GetTemporary(blurRTsize, blurRTsize, 0, RenderTextureFormat.ARGB32);
	            for(int i = 0; i < blurTimes; ++i)
	            {
					if(i == 0)
					{
						src.DiscardContents();
						Graphics.Blit(shadowRT, src, blurMtrl, 1);

						if(i == blurTimes - 1)
						{
							Swap(ref src, ref dest);
						}
					}
					else
					{
		                dest.DiscardContents();
		                Graphics.Blit(src, dest, blurMtrl, 1);

		                if(i != blurTimes - 1)
		                {
							Swap(ref src, ref dest);
		                }
					}
	            }
				shadowRT.MarkRestoreExpected();
	            Graphics.Blit(dest, shadowRT, blurMtrl, 0);
	            RenderTexture.ReleaseTemporary(src);
	            RenderTexture.ReleaseTemporary(dest);
	            Graphics.SetRenderTarget(null);
			}
        }
    }

    private void OnPreRender()
    {
#if UNITY_EDITOR
        CreateSourcePoint();
#endif

        UpdateShadowCamera();
    }

    private void Update()
    {
        UpdateShadowArea();
    }

    private void UpdateShadowArea()
    {
        if (shadowCamera != null)
        {
            if (shadowArea != shadowCamera.orthographicSize)
            {
                shadowCamera.orthographicSize = shadowArea;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(sourcePoint != null)
        {
            sourcePoint.position = thisTransform.position;
            sourcePoint.forward = lightDir;

            if (!Application.isPlaying)
            {
                UpdateShadowCamera();
            }

            UnityEditor.SceneView.RepaintAll();
        }
    }

    public void RotateLightDir()
    {
        if (sourcePoint != null && followingTarget != null)
        {
            UnityEditor.Handles.matrix = Matrix4x4.identity;
            sourcePoint.rotation = UnityEditor.Handles.RotationHandle(sourcePoint.rotation, shadowCameraTransform.position + shadowCameraTransform.forward * shadowCamera.farClipPlane * 0.5f);
            lightDir = sourcePoint.forward;
            if (GUI.changed)
            {
                UnityEditor.Undo.RecordObjects(new Object[]{sourcePoint}, "XKShadow");
                UnityEditor.EditorUtility.SetDirty(sourcePoint);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (shadowCamera != null && followingTarget != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.8f);
            Gizmos.matrix = shadowCamera.cameraToWorldMatrix;
            Vector3 n_p1 = new Vector3(shadowCamera.orthographicSize, shadowCamera.orthographicSize * shadowCamera.aspect, 0); n_p1.z -= shadowCamera.farClipPlane * 0.5f;
            Vector3 n_p2 = new Vector3(shadowCamera.orthographicSize, -shadowCamera.orthographicSize * shadowCamera.aspect, 0); n_p2.z -= shadowCamera.farClipPlane * 0.5f;
            Vector3 n_p3 = new Vector3(-shadowCamera.orthographicSize, -shadowCamera.orthographicSize * shadowCamera.aspect, 0); n_p3.z -= shadowCamera.farClipPlane * 0.5f;
            Vector3 n_p4 = new Vector3(-shadowCamera.orthographicSize, shadowCamera.orthographicSize * shadowCamera.aspect, 0); n_p4.z -= shadowCamera.farClipPlane * 0.5f;
            Gizmos.DrawLine(n_p1, n_p2);
            Gizmos.DrawLine(n_p2, n_p3);
            Gizmos.DrawLine(n_p3, n_p4);
            Gizmos.DrawLine(n_p4, n_p1);
            Vector3 f_p1 = n_p1; f_p1.z = -shadowCamera.farClipPlane;
            Vector3 f_p2 = n_p2; f_p2.z = -shadowCamera.farClipPlane;
            Vector3 f_p3 = n_p3; f_p3.z = -shadowCamera.farClipPlane;
            Vector3 f_p4 = n_p4; f_p4.z = -shadowCamera.farClipPlane;
            Gizmos.DrawLine(f_p1, f_p2);
            Gizmos.DrawLine(f_p2, f_p3);
            Gizmos.DrawLine(f_p3, f_p4);
            Gizmos.DrawLine(f_p4, f_p1);

            Gizmos.DrawLine(n_p1, f_p1);
            Gizmos.DrawLine(n_p2, f_p2);
            Gizmos.DrawLine(n_p3, f_p3);
            Gizmos.DrawLine(n_p4, f_p4);
        }

        UnityEditor.SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        if(debugRT)
        {
            if(shadowRT != null)
            {
                int size = (int)(Screen.height * 0.3f);
                GUI.DrawTexture(new Rect(0, 0, size, size), shadowRT, ScaleMode.ScaleToFit, false);
            }
        }
    }
#endif

	private void Swap<T>(ref T a, ref T b)
	{
		T temp = a;
		a = b;
		b = temp;
	}

    private void OnDestroy()
    {
        if(sourcePoint != null && sourcePoint.gameObject != null)
        {
            DestroyObj(sourcePoint.gameObject);
            sourcePoint = null;
        }

		DestroyShaderAndMaterial();
        DestroyCameraRT();

        if (shadowCameraTransform != null && shadowCameraTransform.gameObject != null)
        {
            DestroyObj(shadowCameraTransform.gameObject);
            shadowCameraTransform = null;
        }

        if(clearCamGo != null)
        {
            DestroyObj(clearCamGo);
            clearCamGo = null;
        }
    }

    private void DestroyObj(Object obj)
    {
#if UNITY_EDITOR
        DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
    }
}
