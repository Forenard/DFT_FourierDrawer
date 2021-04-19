using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;
public class DFT_FourierDrawer : MonoBehaviour
{
    [SerializeField] GameObject _Camera;

    [SerializeField] GameObject InputRender;

    [SerializeField] GameObject OutputRender;

    [SerializeField] GameObject Tip;
    [SerializeField] VisualEffect VisualEffect;
    [SerializeField] ParticleSystem particle;
    [SerializeField] bool IsAnimation = true;
    [SerializeField] int DFTDataSize = 50;

    [SerializeField] int InputRoughness = 10;
    [SerializeField] float AnimationSpeed = 1f;
    private Vector3 beforeInputVec;
    private bool isInputEnd = false;
    private bool isButtonDowned = false;
    private bool isComputed = false;
    private List<Vector3> inputList;

    private List<float> FX = new List<float>();
    private List<float> FZ = new List<float>();
    private List<float> DFT_X_R = new List<float>();
    private List<float> DFT_X_I = new List<float>();
    private List<float> DFT_Z_R = new List<float>();
    private List<float> DFT_Z_I = new List<float>();

    private List<LineRenderer> DrawLines = new List<LineRenderer>();
    private LineRenderer inputLine;
    private int drawInputCount = 0;
    private float elapsedTime = 0;

    private int aio = 0;
    private bool forwardCamera = false;
    private Vector3 first_cam;
    void Start()
    {
        first_cam = _Camera.transform.position;
        particle.Pause();
    }


    void Update()
    {
        if (Input.GetKeyDown("d"))
        {
            Debug.Log(aio);
            DrawAnim(aio);
            aio++;
        }
        Branch();
    }

    void DrawAnim(float t)
    {

        float x = 0;
        float z = 0;
        float w0 = 2 * Mathf.PI * t / (float)(DFTDataSize);
        float[] bef_x = new float[2] { 0, 0 };
        float[] bef_z = new float[2] { 0, 0 };
        for (int i = 0; i < DFTDataSize; i++)
        {
            float _t = w0 * i;
            float _x = DFT_X_R[i] * Mathf.Cos(_t) - DFT_X_I[i] * Mathf.Sin(_t);
            float _z = DFT_Z_R[i] * Mathf.Cos(_t) - DFT_Z_I[i] * Mathf.Sin(_t);

            DrawLines[i].SetPosition(0, new Vector3(x / (float)(DFTDataSize), 0, z / (float)(DFTDataSize)));
            x += _x;
            z += _z;
            DrawLines[i].SetPosition(1, new Vector3(x / (float)(DFTDataSize), 0, z / (float)(DFTDataSize)));
            DrawLines[i].startColor = Color.HSVToRGB((i % 16) / 16f, 1, 1);
            DrawLines[i].endColor = Color.HSVToRGB((i % 16) / 16f, 1, 1);
        }
        x /= (float)(DFTDataSize);
        z /= (float)(DFTDataSize);
        Tip.transform.position = new Vector3(x, 0, z);
        VisualEffect.SetVector3("TipPos", new Vector3(x, 0, z));
        particle.Play();
    }
    void DrawNotAnim()
    {

    }
    void Branch()
    {
        if (isInputEnd)
        {
            if (!isComputed)
            {
                isComputed = true;
                Compute();
            }
            else
            {
                if (IsAnimation)
                {
                    elapsedTime += Time.deltaTime * AnimationSpeed;
                    int t = Mathf.FloorToInt(elapsedTime);
                    if (t >= DFTDataSize - 1)
                    {
                        t = 0;
                        elapsedTime = 0;
                    }
                    DrawAnim(t);
                }
                else
                {
                    DrawNotAnim();
                }
                if (forwardCamera)
                {
                    _Camera.transform.position = new Vector3(Tip.transform.position.x, _Camera.transform.position.y, Tip.transform.position.z);
                    _Camera.GetComponent<Camera>().orthographicSize = 2.5f;
                }
                else
                {
                    _Camera.transform.position = first_cam;
                    _Camera.GetComponent<Camera>().orthographicSize = 5;

                }
            }
        }
        else
        {
            DrawInput();
        }

    }
    public void Change_Cam()
    {
        forwardCamera = !forwardCamera;
    }

    void Compute()
    {
        int _listSize = inputList.Count;


        if (DFTDataSize > _listSize)
        {
            //一辺につき何分割できるか
            int multi = DFTDataSize / _listSize;
            int amari = DFTDataSize % _listSize;
            for (int i = 0; i < _listSize; i++)
            {
                int _loop = i < amari ? multi + 1 : multi;
                Vector3 pnow = inputList[i];
                Vector3 pnext = inputList[(i + 1) % _listSize];
                for (int l = 0; l < _loop; l++)
                {
                    Vector3 _p = Vector3.Lerp(pnow, pnext, l / (float)(_loop));
                    FX.Add(_p.x);
                    FZ.Add(_p.z);
                }
            }
        }
        else
        {
            for (int i = 0; i < DFTDataSize; i++)
            {
                int _i = i * _listSize / DFTDataSize;

                Vector3 _p = inputList[_i];
                float _time = 0;
                if (i != 0)
                {
                    int __i = (i - 1) * _listSize / DFTDataSize;
                    Vector3 __p = inputList[__i];
                    _time = Mathf.Sqrt((_p.x - __p.x) * (_p.x - __p.x) + (_p.z - __p.z) * (_p.z - __p.z));
                }
                FX.Add(_p.x);
                FZ.Add(_p.z);
            }
        }

        float w0 = 2 * Mathf.PI / (float)(DFTDataSize);
        for (int i = 0; i < DFTDataSize; i++)
        {
            DFT_X_R.Add(0);
            DFT_X_I.Add(0);
            DFT_Z_R.Add(0);
            DFT_Z_I.Add(0);
            for (int l = 0; l < DFTDataSize; l++)
            {
                DFT_X_R[i] += FX[l] * Mathf.Cos(-w0 * i * l);
                DFT_X_I[i] += FX[l] * Mathf.Sin(-w0 * i * l);
                DFT_Z_R[i] += FZ[l] * Mathf.Cos(-w0 * i * l);
                DFT_Z_I[i] += FZ[l] * Mathf.Sin(-w0 * i * l);
            }

        }

        for (int i = 0; i < DFTDataSize; i++)
        {
            GameObject _line = GameObject.Instantiate(OutputRender) as GameObject;
            _line.transform.position = Vector3.zero;
            _line.name = i.ToString();
            _line.GetComponent<LineRenderer>().positionCount = 2;
            DrawLines.Add(_line.GetComponent<LineRenderer>());
        }

    }


    void DrawInput()
    {

        if (Input.GetMouseButtonDown(0) && !isButtonDowned)
        {
            GameObject _line = GameObject.Instantiate(InputRender) as GameObject;
            inputLine = _line.GetComponent<LineRenderer>();
            inputList = new List<Vector3>();
            inputLine.positionCount = 0;
            AddLine(LayPoint());
            isButtonDowned = true;
        }
        drawInputCount++;
        if (Input.GetMouseButton(0) && isButtonDowned && (drawInputCount > InputRoughness))
        {
            AddLine(LayPoint());
            drawInputCount = 0;
        }
        else if (Input.GetMouseButtonUp(0) && isButtonDowned)
        {
            isButtonDowned = false;
            isInputEnd = true;
            Destroy(inputLine);

        }


    }
    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ChangeAnimationSpeed(Slider _s)
    {
        this.AnimationSpeed = 300 * (_s.value / 100f);
    }


    void AddLine(Vector3 _vec)
    {
        inputList.Add(_vec);
        inputLine.positionCount += 1;
        inputLine.SetPosition(inputLine.positionCount - 1, _vec);
    }

    /*
    *paletにlayを発射、衝突を検知、位置を返す
    */
    Vector3 LayPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (hit.transform.tag == "palet")
            {
                return hit.point;
            }
        }
        return Vector3.zero;
    }
}
