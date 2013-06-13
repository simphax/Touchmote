// +------------+
// | Directives |
// +------------+
// Specify Windows version
#define WINVER         0x0600
#define _WIN32_WINNT   0x0600

// Includes
#include <time.h>
#include <d3d9.h>
#include <d3dx9.h>
#include <dwmapi.h>

// Import libraries to link with
#pragma comment(lib, "d3d9.lib")
#pragma comment(lib, "d3dx9.lib")
#pragma comment(lib, "dwmapi.lib")

#define ARGB_TRANS   0x00000000 // 100% alpha
#define MAX_CURSORS 16
#define SPRITE_SIZE 128

#define NORMAL_SIZE 0.5f
#define PRESSED_SIZE 0.4f
#define HIDDEN_SIZE 0.0f
#define ANIMATION_DURATION 100


struct D3DCURSOR
{
	FLOAT		x,y,rotation,last_rendered_x,last_rendered_y,scaling,snapshot_scaling;
	BOOL		hidden,enabled,pressed;
	DWORD		color;
	clock_t		animationStart;
};

// +---------+
// | Globals |
// +---------+
WCHAR                   *g_wcpAppName  = L"D3DCursor";
INT                     g_iWidth       = 800;
INT                     g_iHeight      = 600;
MARGINS                 g_mgDWMMargins = {-1, -1, -1, -1};
IDirect3D9Ex            *g_pD3D        = NULL;
IDirect3DDevice9Ex      *g_pD3DDevice  = NULL;
IDirect3DVertexBuffer9  *g_pVB         = NULL;
D3DCURSOR				*cursors = new D3DCURSOR[MAX_CURSORS];
LPD3DXSPRITE g_sprite=NULL;
LPDIRECT3DTEXTURE9 g_gradiantCircle=NULL;
LPDIRECT3DTEXTURE9 g_circle=NULL;
LPDIRECT3DTEXTURE9 g_stroke=NULL;
INT enabledCursors = 0;

FLOAT scale = 0.1f;

  HWND       hWnd  = NULL;

    D3DXMATRIX Identity;



// +--------------+
// | D3DStartup() |
// +--------------+----------------------------------+
// | Initialise Direct3D and perform once only tasks |
// +-------------------------------------------------+
HRESULT D3DStartup(HWND hWnd)
{
	BOOL                  bCompOk             = FALSE;   // Is composition enabled? 
	D3DPRESENT_PARAMETERS pp;                            // Presentation prefs
	DWORD                 msqAAQuality        = 0;       // Non-maskable quality

  
  
	D3DXMATRIX Ortho2D;    

	// Make sure that DWM composition is enabled
	DwmIsCompositionEnabled(&bCompOk);
	if(!bCompOk) return E_FAIL;

	// Create a Direct3D object
	if(FAILED(Direct3DCreate9Ex(D3D_SDK_VERSION, &g_pD3D))) return E_FAIL;

	// Setup presentation parameters
	ZeroMemory(&pp, sizeof(pp));
	pp.Windowed            = TRUE;
	pp.SwapEffect          = D3DSWAPEFFECT_DISCARD; // Required for multi sampling
	pp.BackBufferFormat    = D3DFMT_A8R8G8B8;       // Back buffer format with alpha channel
	
	// Set highest quality non-maskable AA available or none if not
	if(SUCCEEDED(g_pD3D->CheckDeviceMultiSampleType(D3DADAPTER_DEFAULT,
													D3DDEVTYPE_HAL,
													D3DFMT_A8R8G8B8,
													TRUE,
													D3DMULTISAMPLE_NONMASKABLE,
													&msqAAQuality
													)))
	{
	// Set AA quality
	pp.MultiSampleType     = D3DMULTISAMPLE_NONMASKABLE;
	pp.MultiSampleQuality  = msqAAQuality - 1;
	}
	else
	{
	// No AA
	pp.MultiSampleType     = D3DMULTISAMPLE_NONE;
	}

	// Create a Direct3D device object
	if(FAILED(g_pD3D->CreateDeviceEx(D3DADAPTER_DEFAULT,
									D3DDEVTYPE_HAL,
									hWnd,
									D3DCREATE_HARDWARE_VERTEXPROCESSING,
									&pp,
									NULL,
									&g_pD3DDevice
									))) return E_FAIL;

	// Configure the device state
  
	g_pD3DDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	

		D3DXMatrixOrthoLH(&Ortho2D, g_iWidth, g_iHeight, 0.0f, 1.0f);
	D3DXMatrixIdentity(&Identity);

	g_pD3DDevice->SetTransform(D3DTS_PROJECTION, &Ortho2D);
	g_pD3DDevice->SetTransform(D3DTS_WORLD, &Identity);
	g_pD3DDevice->SetTransform(D3DTS_VIEW, &Identity);


	g_pD3DDevice->SetRenderState( D3DRS_ALPHABLENDENABLE, TRUE);

	return S_OK;
}

// +---------------+
// | D3DShutdown() |
// +---------------+----------------------+
// | Release all created Direct3D objects |
// +--------------------------------------+
VOID D3DShutdown(VOID)
{
  if(g_pVB != NULL ) g_pVB->Release();
  if(g_pD3DDevice != NULL) g_pD3DDevice->Release();
  if(g_pD3D != NULL) g_pD3D->Release();
}

// +--------------+
// | CreateCube() |
// +--------------+------------------------------+
// | Populates a vertex buffer with a cube shape |
// +---------------------------------------------+
HRESULT InitSprites(VOID)
{
	if (SUCCEEDED(D3DXCreateSprite(g_pD3DDevice,&g_sprite)))
	{
		// created OK
	}

	D3DXCreateTextureFromFile(g_pD3DDevice,L"circle.png", &g_circle );
	D3DXCreateTextureFromFile(g_pD3DDevice,L"gradiantcircle.png", &g_gradiantCircle );
	D3DXCreateTextureFromFile(g_pD3DDevice,L"stroke.png", &g_stroke );

	return S_OK;
}
FLOAT easeInOutQuint(FLOAT elapsedTime, FLOAT startValue, FLOAT changeInValue, FLOAT duration) {
	elapsedTime /= duration/2;
	if (elapsedTime < 1) return changeInValue/2*elapsedTime*elapsedTime*elapsedTime*elapsedTime*elapsedTime + startValue;
	elapsedTime -= 2;
	return changeInValue/2*(elapsedTime*elapsedTime*elapsedTime*elapsedTime*elapsedTime + 2) + startValue;
};
// +----------+
// | Render() |
// +----------+-------------------------+
// | Renders a scene to the back buffer |
// +------------------------------------+
VOID Render(VOID)
{
  D3DXMATRIX    scaleMatrix;
  D3DXMATRIX	positionMatrix;
  // Sanity check
  if(g_pD3DDevice == NULL) return;
  if(g_sprite == NULL) return;
  
  D3DRECT *clearRect = new D3DRECT[enabledCursors];
  int j = -1;
  for(int i=0; i<enabledCursors; i++)
  {
	  while(!cursors[++j].enabled)
	  {
		
	  }
	  
	  clearRect[i].x1 = cursors[j].last_rendered_x - (SPRITE_SIZE/2);
	  clearRect[i].x2 = cursors[j].last_rendered_x + (SPRITE_SIZE/2);
	  clearRect[i].y1 = cursors[j].last_rendered_y - (SPRITE_SIZE/2);
	  clearRect[i].y2 = cursors[j].last_rendered_y + (SPRITE_SIZE/2);
  }

  g_pD3DDevice->Clear(enabledCursors, clearRect, D3DCLEAR_TARGET, ARGB_TRANS, 1.0f, 0);

  // Render scene
  if(SUCCEEDED(g_pD3DDevice->BeginScene()))
  {
	D3DXVECTOR2 pos;
	RECT size;
	D3DXVECTOR2 spriteCentre = D3DXVECTOR2(64.0f,64.0f);
	D3DXMATRIX mat;
	D3DXVECTOR2 scaling;

	size.top=0;
	size.left=0;
	size.right=128;
	size.bottom=128;

	if(SUCCEEDED(g_sprite->Begin(D3DXSPRITE_ALPHABLEND)))
	{
		j = -1;
		for(int i=0; i<enabledCursors; i++)
		{
			while(!cursors[++j].enabled)
			{
		
			}
			cursors[j].last_rendered_x = cursors[j].x;
			cursors[j].last_rendered_y = cursors[j].y;

			pos.x = cursors[j].x - (SPRITE_SIZE/2);
			pos.y = cursors[j].y - (SPRITE_SIZE/2);



			//Animation
			if(cursors[j].hidden)
			{
				if(abs(cursors[j].scaling - HIDDEN_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[j].scaling = easeInOutQuint(diff,cursors[j].snapshot_scaling,HIDDEN_SIZE-cursors[j].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[j].scaling = HIDDEN_SIZE;
				}
			}
			else if(cursors[j].pressed)
			{
				if(abs(cursors[j].scaling - PRESSED_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[j].scaling = easeInOutQuint(diff,cursors[j].snapshot_scaling,PRESSED_SIZE-cursors[j].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[j].scaling = PRESSED_SIZE;
				}
			}
			else if(!cursors[j].pressed)
			{
				if(abs(cursors[j].scaling - NORMAL_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[j].scaling = easeInOutQuint(diff,cursors[j].snapshot_scaling,NORMAL_SIZE-cursors[j].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[j].scaling = NORMAL_SIZE;
				}
			}

			scaling.x = cursors[j].scaling;
			scaling.y = cursors[j].scaling;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			g_sprite->SetTransform(&mat);
			g_sprite->Draw(g_circle,NULL,NULL,NULL,0xff000000 | cursors[j].color);

			scaling.x *= 0.9f;
			scaling.y *= 0.9f;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			g_sprite->SetTransform(&mat);
			g_sprite->Draw(g_circle,NULL,NULL,NULL,0xff000000);

			scaling.x *= 0.5f;
			scaling.y *= 0.5f;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			g_sprite->SetTransform(&mat);
			g_sprite->Draw(g_circle,NULL,NULL,NULL,0xffFFFFFF);

		}

		g_sprite->End();
	}

	g_pD3DDevice->EndScene();
  }

  // Update display
  g_pD3DDevice->PresentEx(NULL, NULL, NULL, NULL, NULL);
}

// +--------------+
// | WindowProc() |
// +--------------+------------------+
// | The main window message handler |
// +---------------------------------+
LRESULT WINAPI WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
  switch(uMsg)
  {
    case WM_DESTROY:
      // Signal application to terminate
      PostQuitMessage(0);
      return 0;

    case WM_ERASEBKGND:
      // We dont want to call render twice so just force Render() in WM_PAINT to be called
      SendMessage(hWnd, WM_PAINT, NULL, NULL);
      return TRUE;

    case WM_PAINT:
      // Force a render to keep the window updated
		Sleep(10);
      return 0;
  }

  return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

// +-----------+
// | WinMain() |
// +-----------+---------+
// | Program entry point |
// +---------------------+
extern "C" __declspec(dllexport)INT WINAPI RunD3DCursorWindow(HINSTANCE hInstance, HWND hParent, int width, int height)
{
  MSG        uMsg;     
  WNDCLASSEX wc    = {sizeof(WNDCLASSEX),              // cbSize
                      NULL,                            // style
                      WindowProc,                      // lpfnWndProc
                      NULL,                            // cbClsExtra
                      NULL,                            // cbWndExtra
                      hInstance,                       // hInstance
                      LoadIcon(NULL, IDI_APPLICATION), // hIcon
                      LoadCursor(NULL, IDC_ARROW),     // hCursor
                      NULL,                            // hbrBackground
                      NULL,                            // lpszMenuName
                      g_wcpAppName,                    // lpszClassName
                      LoadIcon(NULL, IDI_APPLICATION)};// hIconSm

  RegisterClassEx(&wc);

  g_iWidth = width;
  g_iHeight = height;

  hWnd = CreateWindowEx(WS_EX_COMPOSITED | WS_EX_LAYERED | WS_EX_TRANSPARENT,             // dwExStyle
                        g_wcpAppName,                 // lpClassName
                        g_wcpAppName,                 // lpWindowName
						WS_POPUP,        // dwStyle
                        0, 0, // x, y
                        g_iWidth, g_iHeight,          // nWidth, nHeight
                        hParent,                         // hWndParent
                        NULL,                         // hMenu
                        hInstance,                    // hInstance
                        NULL);                        // lpParam

  // Extend glass to cover whole window
  DwmExtendFrameIntoClientArea(hWnd, &g_mgDWMMargins);

  SetLayeredWindowAttributes(hWnd, 0, 180, LWA_ALPHA);
 
  SetWindowPos(hWnd,HWND_TOPMOST,0,0,g_iWidth,g_iHeight,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE);

  // Initialise Direct3D
  if(SUCCEEDED(D3DStartup(hWnd)))
  {
    if(SUCCEEDED(InitSprites()))
    {
      // Show the window
      ShowWindow(hWnd, SW_SHOWDEFAULT);
      UpdateWindow(hWnd);

      // Enter main loop
      while(TRUE)
      {
        // Check for a message
        if(PeekMessage(&uMsg, NULL, 0, 0, PM_REMOVE))
        {
          // Check if the message is WM_QUIT
          if(uMsg.message == WM_QUIT)
          {
            // Break out of main loop
            break;
          }

          // Pump the message
          TranslateMessage(&uMsg);
          DispatchMessage(&uMsg);
        }

        // Render a frame
        //Render();
		Sleep(10);
      }
    }
  }

  // Shutdown Direct3D
  D3DShutdown();

  // Exit application
  return 0;
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorWindowSize(int width, int height)
{
	SetWindowPos(hWnd,HWND_TOPMOST,0,0,g_iWidth,g_iHeight,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE);
}

extern "C" __declspec(dllexport)VOID WINAPI RenderAllD3DCursors()
{
	try {
	Render();
	} catch(...) {}
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorPosition(int id, int x, int y)
{
	cursors[id].x = x;
	cursors[id].y = y;
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorPressed(int id, bool pressed)
{
	if(cursors[id].pressed != pressed)
	{
		cursors[id].pressed = pressed;
		cursors[id].animationStart = clock();
		cursors[id].snapshot_scaling = cursors[id].scaling;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorHidden(int id, bool hidden)
{
	if(cursors[id].hidden != hidden)
	{
		cursors[id].hidden = hidden;
		cursors[id].animationStart = clock();
		cursors[id].snapshot_scaling = cursors[id].scaling;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI AddD3DCursor(int id, DWORD color)
{
	if(id >= MAX_CURSORS)
	{

	}
	else
	{
		D3DCURSOR newcursor;

		newcursor.x = 0;
		newcursor.y = 0;
		newcursor.rotation = 0;
		newcursor.last_rendered_x = 0;
		newcursor.last_rendered_y = 0;
		newcursor.scaling = 0.5f;
		newcursor.hidden = false;
		newcursor.enabled = true;
		newcursor.color = color;

		cursors[id] = newcursor;

		enabledCursors++;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI RemoveD3DCursor(int id)
{
	cursors[id].enabled = false;
	enabledCursors--;
}