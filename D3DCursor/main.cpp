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

#define TEXTURE_PATH L"Resources\\circle.png"


struct D3DCURSOR
{
	HWND		window;
	IDirect3D9Ex            *D3DEx;
	IDirect3DDevice9Ex      *D3DDevice;
	LPD3DXSPRITE	sprite;
	LPDIRECT3DTEXTURE9	texture;
	INT			width,height;
	FLOAT		x,y,rotation,last_rendered_x,last_rendered_y,scaling,snapshot_scaling;
	BOOL		hidden,enabled,pressed;
	DWORD		color;
	clock_t		animationStart;
};

struct CURSORPTR
{
	D3DCURSOR *cursor;
};

// +---------+
// | Globals |
// +---------+
//WCHAR                   *g_wcpAppName  = L"D3DCursor";
//INT                     g_iWidth       = 800;
//INT                     g_iHeight      = 600;
MARGINS                 g_mgDWMMargins = {-1, -1, -1, -1};
//IDirect3D9Ex            *g_pD3D        = NULL;
//IDirect3DDevice9Ex      *g_pD3DDevice  = NULL;
//IDirect3DVertexBuffer9  *g_pVB         = NULL;
D3DCURSOR				*cursors = new D3DCURSOR[MAX_CURSORS];
//LPD3DXSPRITE g_sprite=NULL;
//LPDIRECT3DTEXTURE9 g_circle=NULL;
INT enabledCursors = 0;

FLOAT scale = 0.1f;

/*HWND hWnd  = NULL;*/

D3DXMATRIX Identity;

//CURSORPTR				*clearQueue = new CURSORPTR[MAX_CURSORS];
//INT nToClear=0;

//IDirect3DQuery9 *pOcclusionQuery;
DWORD numberOfPixelsDrawn;


// +--------------+
// | D3DStartup() |
// +--------------+----------------------------------+
// | Initialise Direct3D and perform once only tasks |
// +-------------------------------------------------+
HRESULT D3DStartup(int id)
{
	BOOL                  bCompOk             = FALSE;   // Is composition enabled? 
	D3DPRESENT_PARAMETERS pp;                            // Presentation prefs
	DWORD                 msqAAQuality        = 0;       // Non-maskable quality

	D3DXMATRIX Ortho2D;    

	// Make sure that DWM composition is enabled
	DwmIsCompositionEnabled(&bCompOk);
	if(!bCompOk) return E_FAIL;

	// Create a Direct3D object
	if(FAILED(Direct3DCreate9Ex(D3D_SDK_VERSION, &cursors[id].D3DEx))) return E_FAIL;

	// Setup presentation parameters
	ZeroMemory(&pp, sizeof(pp));
	pp.Windowed            = TRUE;
	pp.SwapEffect          = D3DSWAPEFFECT_DISCARD; // Required for multi sampling
	pp.BackBufferFormat    = D3DFMT_A8R8G8B8;       // Back buffer format with alpha channel
	
	// Set highest quality non-maskable AA available or none if not
	if(SUCCEEDED(cursors[id].D3DEx->CheckDeviceMultiSampleType(D3DADAPTER_DEFAULT,
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
	if(FAILED(cursors[id].D3DEx->CreateDeviceEx(D3DADAPTER_DEFAULT,
									D3DDEVTYPE_HAL,
									cursors[id].window,
									D3DCREATE_HARDWARE_VERTEXPROCESSING,
									&pp,
									NULL,
									&cursors[id].D3DDevice
									))) return E_FAIL;

	// Configure the device state
  
	cursors[id].D3DDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	D3DXMatrixOrthoLH(&Ortho2D, cursors[id].width, cursors[id].height, 0.0f, 1.0f);
	D3DXMatrixIdentity(&Identity);

	cursors[id].D3DDevice->SetTransform(D3DTS_PROJECTION, &Ortho2D);
	cursors[id].D3DDevice->SetTransform(D3DTS_WORLD, &Identity);
	cursors[id].D3DDevice->SetTransform(D3DTS_VIEW, &Identity);

	cursors[id].D3DDevice->SetMaximumFrameLatency(1);
	cursors[id].D3DDevice->SetGPUThreadPriority(7);

	//cursors[id].D3DDevice->CreateQuery(D3DQUERYTYPE_OCCLUSION, &pOcclusionQuery);


	cursors[id].D3DDevice->SetRenderState( D3DRS_ALPHABLENDENABLE, TRUE);

	return S_OK;
}

// +---------------+
// | D3DShutdown() |
// +---------------+----------------------+
// | Release all created Direct3D objects |
// +--------------------------------------+
VOID D3DShutdown(INT id)
{
  //if(g_pVB != NULL ) g_pVB->Release();
  if(cursors[id].D3DDevice != NULL) cursors[id].D3DDevice->Release();
  if(cursors[id].D3DEx != NULL) cursors[id].D3DEx->Release();
}

HRESULT InitSprites(int id)
{
	if (SUCCEEDED(D3DXCreateSprite(cursors[id].D3DDevice,&cursors[id].sprite)))
	{
		// created OK
	}

	D3DXCreateTextureFromFile(cursors[id].D3DDevice,TEXTURE_PATH, &cursors[id].texture );

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
VOID Render(int id)
{
  D3DXMATRIX    scaleMatrix;
  D3DXMATRIX	positionMatrix;
  // Sanity check
  if(cursors[id].D3DDevice == NULL) return;
  if(cursors[id].texture == NULL) return;
  //if(pOcclusionQuery == NULL) return;

  /*
  D3DRECT *clearRect = new D3DRECT[enabledCursors+nToClear];
  int j = -1;
  int i = 0;
  for(i=0; i<enabledCursors; i++)
  {
	  while(!cursors[++j].enabled)
	  {
		
	  }
	  
	  clearRect[i].x1 = cursors[j].last_rendered_x - (SPRITE_SIZE/2);
	  clearRect[i].x2 = cursors[j].last_rendered_x + (SPRITE_SIZE/2);
	  clearRect[i].y1 = cursors[j].last_rendered_y - (SPRITE_SIZE/2);
	  clearRect[i].y2 = cursors[j].last_rendered_y + (SPRITE_SIZE/2);
  }
  for(j=0; i<enabledCursors+nToClear; i++)
  {
	  clearRect[i].x1 = clearQueue[j].cursor->last_rendered_x - (SPRITE_SIZE/2);
	  clearRect[i].x2 = clearQueue[j].cursor->last_rendered_x + (SPRITE_SIZE/2);
	  clearRect[i].y1 = clearQueue[j].cursor->last_rendered_y - (SPRITE_SIZE/2);
	  clearRect[i].y2 = clearQueue[j].cursor->last_rendered_y + (SPRITE_SIZE/2);
	  j++;
  }

  g_pD3DDevice->Clear(enabledCursors+nToClear, clearRect, D3DCLEAR_TARGET, ARGB_TRANS, 1.0f, 0);
  
  nToClear=0;
  */
  cursors[id].D3DDevice->Clear(0, NULL, D3DCLEAR_TARGET, ARGB_TRANS, 1.0f, 0);//Clear everything

  // Render scene
  if(SUCCEEDED(cursors[id].D3DDevice->BeginScene()))
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

	if(SUCCEEDED(cursors[id].sprite->Begin(D3DXSPRITE_ALPHABLEND)))
	{
		/*int j = -1;
		for(int i=0; i<enabledCursors; i++)
		{
			while(!cursors[++j].enabled)
			{
		
			}*/
			//cursors[id].last_rendered_x = cursors[id].x;
			//cursors[id].last_rendered_y = cursors[id].y;

			pos.x = -(SPRITE_SIZE/2)/2;
			pos.y = -(SPRITE_SIZE/2)/2;


			//Animation
			if(cursors[id].hidden)
			{
				if(abs(cursors[id].scaling - HIDDEN_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[id].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[id].scaling = easeInOutQuint(diff,cursors[id].snapshot_scaling,HIDDEN_SIZE-cursors[id].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[id].scaling = HIDDEN_SIZE;
				}
			}
			else if(cursors[id].pressed)
			{
				if(abs(cursors[id].scaling - PRESSED_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[id].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[id].scaling = easeInOutQuint(diff,cursors[id].snapshot_scaling,PRESSED_SIZE-cursors[id].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[id].scaling = PRESSED_SIZE;
				}
			}
			else if(!cursors[id].pressed)
			{
				if(abs(cursors[id].scaling - NORMAL_SIZE) > 0.01)
				{
					float diff = (((float)clock() - (float)cursors[id].animationStart) / CLOCKS_PER_SEC ) * 1000;
					cursors[id].scaling = easeInOutQuint(diff,cursors[id].snapshot_scaling,NORMAL_SIZE-cursors[id].snapshot_scaling,ANIMATION_DURATION);
				}
				else
				{
					cursors[id].scaling = NORMAL_SIZE;
				}
			}

			if(cursors[id].scaling < 0 || cursors[id].scaling > 1.0f)
			{
				cursors[id].scaling = HIDDEN_SIZE;
			}

			scaling.x = cursors[id].scaling;
			scaling.y = cursors[id].scaling;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			cursors[id].sprite->SetTransform(&mat);
			cursors[id].sprite->Draw(cursors[id].texture,NULL,NULL,NULL,0xff000000 | cursors[id].color);

			scaling.x *= 0.9f;
			scaling.y *= 0.9f;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			cursors[id].sprite->SetTransform(&mat);
			cursors[id].sprite->Draw(cursors[id].texture,NULL,NULL,NULL,0xff000000);

			scaling.x *= 0.5f;
			scaling.y *= 0.5f;
			D3DXMatrixTransformation2D(&mat,&spriteCentre,0.0,&scaling,&spriteCentre,0,&pos);
			cursors[id].sprite->SetTransform(&mat);
			cursors[id].sprite->Draw(cursors[id].texture,NULL,NULL,NULL,0xffFFFFFF);

		//}

		cursors[id].sprite->End();
	}

	cursors[id].D3DDevice->EndScene();
  }
  
  // Update display
  cursors[id].D3DDevice->PresentEx(NULL, NULL, NULL, NULL, NULL);

  /*pOcclusionQuery->Issue(D3DISSUE_END);
  while(S_FALSE == pOcclusionQuery->GetData( &numberOfPixelsDrawn, sizeof(DWORD), D3DGETDATA_FLUSH ))
		;
		*/
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
		int j = -1;
		for(int i=0; i<enabledCursors; i++)
		{
			while(!cursors[++j].enabled)
			{
		
			}
			if(cursors[j].window == hWnd)
			{
				SetWindowPos(cursors[j].window,HWND_TOPMOST,cursors[j].x-(SPRITE_SIZE/2)/2,cursors[j].y-(SPRITE_SIZE/2)/2,cursors[j].width,cursors[j].height,SWP_NOSIZE|SWP_NOACTIVATE);
				Render(j);
				return DefWindowProc(hWnd, uMsg, wParam, lParam);
			}
		}
		return 0;
  }
  
  return DefWindowProc(hWnd, uMsg, wParam, lParam);
}


// +-----------+
// | WinMain() |
// +-----------+---------+
// | Program entry point |
// +---------------------+
extern "C" __declspec(dllexport)HWND WINAPI NewD3DCursorWindow(int id, HINSTANCE hInstance, HWND hParent)
{
  MSG        uMsg;   
  LPCWSTR windowName = L"Cursor "+id;
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
                      windowName,                    // lpszClassName
                      LoadIcon(NULL, IDI_APPLICATION)};// hIconSm

  RegisterClassEx(&wc);

  cursors[id].width = (SPRITE_SIZE/2);
  cursors[id].height = (SPRITE_SIZE/2);

  cursors[id].window = CreateWindowEx(WS_EX_COMPOSITED | WS_EX_LAYERED | WS_EX_TRANSPARENT,             // dwExStyle
                        windowName,                 // lpClassName
                        windowName,                 // lpWindowName
						WS_POPUP,        // dwStyle
                        0, 0, // x, y
                        cursors[id].width, cursors[id].height,          // nWidth, nHeight
                        hParent,                         // hWndParent
                        NULL,                         // hMenu
                        hInstance,                    // hInstance
                        NULL);                        // lpParam

  // Extend glass to cover whole window
  DwmExtendFrameIntoClientArea(cursors[id].window, &g_mgDWMMargins);

  SetLayeredWindowAttributes(cursors[id].window, 0, 180, LWA_ALPHA);
 
  SetWindowPos(cursors[id].window,HWND_TOPMOST,0,0,cursors[id].width,cursors[id].height,SWP_NOSIZE|SWP_NOACTIVATE);

  // Initialise Direct3D
  if(SUCCEEDED(D3DStartup(id)))
  {
    if(SUCCEEDED(InitSprites(id)))
    {
      // Show the window
      ShowWindow(cursors[id].window, SW_SHOWDEFAULT);
      UpdateWindow(cursors[id].window);
    }
  }

  // Shutdown Direct3D
  //D3DShutdown();

  // Exit application
  return 0;
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorWindowSize(int width, int height)
{
	//SetWindowPos(hWnd,HWND_TOPMOST,0,0,g_iWidth,g_iHeight,SWP_NOSIZE|SWP_NOACTIVATE);
}

extern "C" __declspec(dllexport)VOID WINAPI RenderAllD3DCursors()
{
	int j = -1;
	for(int i=0; i<enabledCursors; i++)
		{
			while(!cursors[++j].enabled)
			{
		
			}
			if(cursors[j].window != NULL)
			{
				SendMessage(cursors[j].window, WM_PAINT, NULL, NULL);
			}
	}
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

extern "C" __declspec(dllexport)VOID WINAPI AddD3DCursor(INT id, DWORD color, HINSTANCE hInstance, HWND hParent)
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
		newcursor.scaling = NORMAL_SIZE;
		newcursor.hidden = false;
		newcursor.enabled = true;
		newcursor.color = color;

		cursors[id] = newcursor;

		newcursor.window = NewD3DCursorWindow(id,hInstance,hParent);

		enabledCursors++;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI RemoveD3DCursor(INT id)
{
	CloseWindow(cursors[id].window);
	cursors[id].enabled = false;
	enabledCursors--;
	//clearQueue[nToClear].cursor = &cursors[id];
	//nToClear++;
}