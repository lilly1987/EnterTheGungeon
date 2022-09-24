using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BepInPluginSample
{
    [BepInPlugin("Game.Lilly.Plugin", "Lilly", "2.1.9.0")]
    public class Lilly : BaseUnityPlugin
    {
        public static ManualLogSource logger;

        static Harmony harmony;

        public ConfigEntry<BepInEx.Configuration.KeyboardShortcut> ShowCounter;
        public ConfigEntry<BepInEx.Configuration.KeyboardShortcut> ShowCounter2;

        private ConfigEntry<bool> isGUIOn;
        private ConfigEntry<bool> isOpen;
        private ConfigEntry<float> uiW;
        private ConfigEntry<float> uiH;


        public int windowId = 542;
        public Rect windowRect;

        public string title = "";
        public string windowName = ""; // 변수용 
        public string FullName = "Plugin"; // 창 펼쳤을때
        public string ShortName = "P"; // 접었을때

        GUILayoutOption h;
        GUILayoutOption w;
        public Vector2 scrollPosition;

        // ==================

        public static ConfigEntry<bool> noAmmo;
        public static string stringToEdit = "only num";
        public static string myitem = "myitem.txt";


        public void Awake()
        {
            logger = Logger;
            Logger.LogMessage("Awake");

            ShowCounter = Config.Bind("GUI", "isGUIOnKey", new KeyboardShortcut(KeyCode.Keypad0));// 이건 단축키
            ShowCounter2 = Config.Bind("GUI", "isOpenKey", new KeyboardShortcut(KeyCode.KeypadPeriod));// 이건 단축키

            isGUIOn = Config.Bind("GUI", "isGUIOn", true);
            isOpen = Config.Bind("GUI", "isOpen", true);
            isOpen.SettingChanged += IsOpen_SettingChanged;
            uiW = Config.Bind("GUI", "uiW", 300f);
            uiH = Config.Bind("GUI", "uiH", 600f);

            if (isOpen.Value)
                windowRect = new Rect(Screen.width - 65, 0, uiW.Value, 800);
            else
                windowRect = new Rect(Screen.width - uiW.Value, 0, uiW.Value, 800);

            IsOpen_SettingChanged(null, null);

            //================
            noAmmo = Config.Bind("GUI", "noAmmo", true);
            SetMyItemMake();
        }

        public void IsOpen_SettingChanged(object sender, EventArgs e)
        {
            logger.LogInfo($"IsOpen_SettingChanged {isOpen.Value} , {isGUIOn.Value},{windowRect.x} ");
            if (isOpen.Value)
            {
                title = ShowCounter.Value.ToString() + "," + ShowCounter2.Value.ToString();
                h = GUILayout.Height(uiH.Value);
                w = GUILayout.Width(uiW.Value);
                windowName = FullName;
                windowRect.x -= (uiW.Value - 64);
            }
            else
            {
                title = "";
                h = GUILayout.Height(40);
                w = GUILayout.Width(60);
                windowName = ShortName;
                windowRect.x += (uiW.Value - 64);
            }
        }


        public void OnEnable()
        {
            Logger.LogWarning("OnEnable");
            // 하모니 패치
            try // 가급적 try 처리 해주기. 하모니 패치중에 오류나면 다른 플러그인까지 영향 미침
            {
                harmony = Harmony.CreateAndPatchAll(typeof(Lilly));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void Update()
        {
            if (ShowCounter.Value.IsUp())// 단축키가 일치할때
            {
                isGUIOn.Value = !isGUIOn.Value;
            }
            if (ShowCounter2.Value.IsUp())// 단축키가 일치할때
            {
                isOpen.Value = !isOpen.Value;
            }
        }


        public void OnGUI()
        {
            if (!isGUIOn.Value)
                return;

            // 창 나가는거 방지
            windowRect.x = Mathf.Clamp(windowRect.x, -windowRect.width + 4, Screen.width - 4);
            windowRect.y = Mathf.Clamp(windowRect.y, -windowRect.height + 4, Screen.height - 4);
            windowRect = GUILayout.Window(windowId, windowRect, WindowFunction, windowName, w, h);
        }

        int k;

        public virtual void WindowFunction(int id)
        {
            GUI.enabled = true; // 기능 클릭 가능

            GUILayout.BeginHorizontal();// 가로 정렬
                                        // 라벨 추가
                                        //GUILayout.Label(windowName, GUILayout.Height(20));
                                        // 안쓰는 공간이 생기더라도 다른 기능으로 꽉 채우지 않고 빈공간 만들기
            if (isOpen.Value) GUILayout.Label(title);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(20))) { isOpen.Value = !isOpen.Value; }
            if (GUILayout.Button("x", GUILayout.Width(20), GUILayout.Height(20))) { isGUIOn.Value = false; }
            GUI.changed = false;

            GUILayout.EndHorizontal();// 가로 정렬 끝

            if (!isOpen.Value) // 닫혔을때
            {
            }
            else // 열렸을때
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

                // 여기에 항목 작성

                if (GUILayout.Button( "Spawn Chosen Items"))
                {
                    SetItem();
                }
                /*
                if (GUILayout.Button($"탄약 소모 없음 { noAmmo.Value}")){ noAmmo.Value = !noAmmo.Value;  }
                */
                if (GUILayout.Button("my item set"))
                {
                    SetMyItem();
                }
                myitem = GUILayout.TextField(myitem);

                GUILayout.Label(" --- ");

                if (GUILayout.Button( "Give Junkan"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(580).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "Give Heart"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(85).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                if (GUILayout.Button( "MAX 9 Heart"))
                {
                    SetHealthMaximum();
                }
                GUILayout.EndHorizontal();

                //GUILayout.Label(" --- ");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "Give Key"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                if (GUILayout.Button( "Set 99 Key"))
                {
                    SetMaxBullets();
                }
                GUILayout.EndHorizontal();

                //GUILayout.Label(" --- ");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "Give Blank"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                if (GUILayout.Button( "Set 99 Blank"))
                {
                    SetBlank99();
                }
                GUILayout.EndHorizontal();
                if (GUILayout.Button( "Blank!"))
                {
                    SetForceBlank();
                }

                //GUILayout.Label(" --- ");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "Give Armor"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                if (GUILayout.Button( "Set 99 Armor"))
                {
                    SetArmor99();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(" --- Reward --- ");

                //상자
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "D"))
                {
                    SetChest(GameManager.Instance.RewardManager.D_Chest);
                }
                if (GUILayout.Button( "C"))
                {
                    SetChest(GameManager.Instance.RewardManager.C_Chest);
                }
                if (GUILayout.Button( "B"))
                {
                    SetChest(GameManager.Instance.RewardManager.B_Chest);
                }
                if (GUILayout.Button( "A"))
                {
                    SetChest(GameManager.Instance.RewardManager.A_Chest);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "S"))
                {
                    SetChest(GameManager.Instance.RewardManager.S_Chest);
                }
                if (GUILayout.Button( "RB"))
                {
                    SetChest(GameManager.Instance.RewardManager.Rainbow_Chest);
                }
                if (GUILayout.Button( "SNG"))
                {
                    SetChest(GameManager.Instance.RewardManager.Synergy_Chest);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(" --- ");

                if (GUILayout.Button( "Give 50 Casings"))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(74).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                if (GUILayout.Button("Set Max Currency"))
                {
                    SetMaxCurrency();
                }

                if (GUILayout.Button("Give Hegemony 50 Credit"))
                {
                    for (int j = 0; j < 50; j++)
                    {
                        LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(297).gameObject, GameManager.Instance.PrimaryPlayer, true);
                    }
                }

                if (GUILayout.Button( "아이템99개 소지"))
                {
                    SetMAX_HELD9();
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "저주 제거"))
                {
                    SetClearCurse();
                }

                if (GUILayout.Button( "저주템"))
                {
                    SetMyItemCurse();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button( "Drop Passive all"))
                {
                    SetDropPassive(true);
                }
                if (GUILayout.Button( "Drop Passive last"))
                {
                    SetDropPassive(false);
                }
                GUILayout.EndHorizontal();


                GUILayout.Label(" --- ");
                stringToEdit = GUILayout.TextField( stringToEdit);

                if (GUILayout.Button( "Give set id") && int.TryParse(stringToEdit, out k))
                {
                    LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(k).gameObject, GameManager.Instance.PrimaryPlayer, true);
                }
                GUILayout.Label(" --- ");


                if (GUILayout.Button( "탄환의 주인"))
                {
                    SetMyItem2();
                }

                if (GUILayout.Button( "item list out"))
                {
                    SetListOut();
                }






                GUILayout.EndScrollView();
            }
            GUI.enabled = true;
            GUI.DragWindow(); // 창 드레그 가능하게 해줌. 마지막에만 넣어야함
        }

        public void OnDisable()
        {
            Logger.LogWarning("OnDisable");
            harmony?.UnpatchSelf();
        }

        // ====================== 하모니 패치 샘플 ===================================
        /*
         
        [HarmonyPatch(typeof(XPPicker), MethodType.Constructor)]
        [HarmonyPostfix]
        public static void XPPickerCtor(ref float ___pickupRadius)
        {
            //logger.LogWarning($"XPPicker.ctor {___pickupRadius}");
            ___pickupRadius = pickupRadius.Value;
        }


        */
        // =========================================================


        [HarmonyPatch(typeof(Gun), "LoseAmmo")]
        [HarmonyPrefix]
        public static void LoseAmmo(ref int amt)
        {
            if (!noAmmo.Value)
            {
                return;
            }
            logger.LogMessage("LoseAmmo {amt}");
            amt = 0;
        }

        public static void SetMyItem2()
        {
            // 탄환의 주인
            int[] array2 = new int[]
            {
            469,471,468,470,467,348,351,349,350
            };
            for (int i = 0; i < array2.Length; i++)
            {
                LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(array2[i]).gameObject, GameManager.Instance.PrimaryPlayer, true);
            }
        }

        public static void SetListOut()
        {
            FileStream fs = new FileStream("PickupObjectDatabase.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("start:" + PickupObjectDatabase.Instance.Objects.Count);
            UnityEngine.Debug.Log("start:" + PickupObjectDatabase.Instance.Objects.Count);
            for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
            {
                if (PickupObjectDatabase.Instance.Objects[i] != null)
                {
                    sw.WriteLine(i + "\t" +
                        PickupObjectDatabase.Instance.Objects[i].EncounterNameOrDisplayName + "\t" +
                        PickupObjectDatabase.Instance.Objects[i].DisplayName + "\t" +
                        PickupObjectDatabase.Instance.Objects[i].name
                        );
                    UnityEngine.Debug.Log(i + "\t" + PickupObjectDatabase.Instance.Objects[i].EncounterNameOrDisplayName);
                }
            }
            sw.WriteLine("end");
            UnityEngine.Debug.Log("end");
            sw.Close();
            fs.Close();
            // PickupObjectDatabase.Instance.InternalGetById(id).DisplayName;
        }

        public static void SetChest(Chest chest)
        {
            IntVector2 basePosition = new IntVector2(
                (int)GameManager.Instance.PrimaryPlayer.transform.position.x
                , (int)GameManager.Instance.PrimaryPlayer.transform.position.y);
            Chest.Spawn(chest, basePosition);
        }

        public static void SetItem()
        {
            int[] array = new int[]
    {
            131,//유틸리티 벨트
			102,
            212,
            115,
            309,
            457,
            170,
            281,
            260,
            262,
            263,
            264,
            466,
            269,
            270,
            307,
            343,
            437,
            452,
            454,
            490,
            491,
            492,
            249,
            301,
            318,
            442,
            232,
            451,
            461,
            494,
            434,
            271,
            529,
            572,
            564,
            580,
            526,
            641
    };


            for (int i = 0; i < array.Length; i++)
            {
                LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(array[i]).gameObject, GameManager.Instance.PrimaryPlayer, true);
            }
        }

        public static void SetMyItemMake()
        {
            FileInfo fileInfo = new FileInfo(myitem);
            //파일 있는지 확인 있을때(true), 없으면(false)
            if (!fileInfo.Exists)
            {
                using (FileStream fs = new FileStream(myitem, FileMode.Create))
                {
                    using (StreamWriter sr = new StreamWriter(fs))
                    {
                        sr.Write(
                            @"
131//유틸리티 벨트
,273//레이저 조준기
,134 //탄띠
,634//크라이시스 스톤
,285//+1 총탄
,113//로켓 추진식 총탄
,298//전격탄
,638 //디볼버 라운드
,640 //보팔 총탄
,655 //배고픈 총탄
,288 //반사식 총탄
,304 //폭발성 탄약
,373 //알파 총탄
,374 //오메가 총탄
,204 //방사능 처리 납
,295 //뜨거운 총탄
,410 //배터리 총탄
,278 //서리 총탄
,527//매력탄
,533 //매력탄
//, //공포탄 총탄
,284// 자동유도 탄환
,352//그림자 탄환
,375 //간편 재장전 총탄
,635 //스노우총탄
//,//원격 총탄
,528 //좀비탄
//대공포탄
,538// 은탄
,532//도금 총탄
,627//백금 총알
,569//혼돈 탄환
//,521// 확률탄
,630//붕총탄
,114//바이오닉 레그
,427//산탄총 커피
,426//산탄 콜라
,212//탄도 장화
,110//매직 스위트
,187//요령 좋은 성격
,435//콧털
,213//리치의 집게손가락
,815//리치의 눈 총탄
,353//분노의 사진
,115//투표 용지
,414//인간 탄환
,354//군사 훈련
,463//쥐갈공명의 반지
,112//지도 제작자의 반지
,158//구덩이 군주의 부적
,191//화염 저항의 반지
,495//총기옥
,488//상자 집착증 반지
,309//클로런시 반지
,174//상자 우정 반지
,254//상자 우정 반지
,294//흉내내기 우정 반지
,456//방아쇠 반지
,159//건드로메다 질병
,258//브로콜리
,431//발키리 용액
,167//핏빛 눈알
,160//건나이트 투구
,161//건나이트 정강이받이
,162//건나이트 건틀렛
,163//건나이트 갑옷
,219//늙은 기사의 방패
,222//늙은 기사의 투구
,305//오래된 문장
,457//가시 갑옷
,564//풀메탈재킷
,256//무거운 장화
,193//맹독충 장화
,315//건부츠
,526//뿅뿅 부츠
//,667//쥐 장화. 엘리베이터 불가
,214//동전 왕관
,165//기름칠한 실린더
,170//아이스큐브
,190//롤링 아이
,135//전쟁의 톱니바퀴
,119//메트로놈
,138//벌집
,137//지도
,281//총굴 청사진
,253//총굴 고추
,259//항체
,262//화이트 구온석
,263//오렌지 구온석
,264//클리어 구온석
,466//그린 구온석
,260//핑크 구온석
,269//레드 구온석
,270//블루 구온석
,565//유리 구온석
//핫뜨거 손목시계
,280//드럼 클립
,287//백업용 총
,290//선글라스
,293//흉내내기 이빨 목걸이
,307//왁스 날개
,312//블래스트 투구
,313//몬스터 혈액
,314//나노머신
,289//칠 잎 클로버
,326//이인자
,321//황금 탄환 부적
,325//혼돈 탄환 부적
,322//로드스톤 탄환 부적
,342//우라늄 탄환 부적
,343//구리 탄환 부적
,344//서리 탄환 부적
,396//테이블 기술 - 마음의 눈
,397//테이블 기술 - 자금
,398//테이블 기술 - 로켓
,633//테이블 기술 - 산탄총
,666//테이블 기술 - 히트
,399//테이블 기술 - 분노
,400//테이블 기술 - 공포탄
,465//테이블 기술 - 기절
,421//하트 권총집
,423//하트 목걸이
,424//하트 보틀
,425//하트 지갑
,440//루비 팔찌
,409//고장 난 텔레비전
,364//얼음 심장
,255//고대 영웅의 반다나
,436//피묻은 목도리
,437//근이완제
,500//힙 홀스터
,311//복제인간
//,452//스펀지
,453//방독면
,454//방호복
,487//상자 해부학
,166//쉘레톤 열쇠
,490//금전 벽돌
,529//전투 깃발
,491//조력자
,492//늑대
,300//개
,249//올빼미
,301//슈퍼 스페이스 터틀
,127//뻥이에요
,148//고물
,641//황금 고물
,580//쓰레기
,318//R2G2
,818//어린 쉘레톤
,442//배지
,572//닭피리
,232//우주의 친구
,451//돼지→영웅 돼지
,632//칠면조
,664//어린 흉내쟁이
,461//공포탄 동료의 반지
,493//현금 가방
,494//은하 용맹 훈장
,434//총탄 우상
,271//납 어레미
,570//노란 약실.저주2
,817//고양이 총탄 킹 왕좌
//,822//카타나 총탄. 저주1
"
                            );
                    }
                }
            }
        }

        public static void SetMyItem()
        {
            List<int> my2 = new List<int>();
            try
            {
                Debug.Log("item:" + myitem);
                using (FileStream fs = new FileStream(myitem, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        Regex r = new Regex("//.*");
                        string l, t;
                        string[] s;
                        int v;
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        while ((l = sr.ReadLine()) != null)
                        {
                            //Debug.Log("item l:" + l);
                            t = r.Replace(l, String.Empty);
                            //Debug.Log("item t:" + t);
                            s = t.Split(new char[] { ' ', '\t', ',' });
                            foreach (var i in s)
                            {
                                //Debug.Log("item i:" + i);
                                if (int.TryParse(i, out v))
                                {
                                    Debug.Log("item v:" + v);
                                    my2.Add(v);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("item:" + e);
                // throw;
            }
            Debug.Log("item my:" + my2.Count);
            //Debug.Log("item my:" + my2.ToString());
            /*
                        int[] my = new int[]
                        {
                            131//유틸리티 벨트
                            ,273//레이저 조준기
                            ,134 //탄띠
                            ,634//크라이시스 스톤
                            ,285//+1 총탄
                            ,113//로켓 추진식 총탄
                            ,298//전격탄
                            ,638 //디볼버 라운드
                            ,640 //보팔 총탄
                            ,655 //배고픈 총탄
                            ,288 //반사식 총탄
                            ,304 //폭발성 탄약
                            ,373 //알파 총탄
                            ,374 //오메가 총탄
                            ,204 //방사능 처리 납
                            ,295 //뜨거운 총탄
                            ,410 //배터리 총탄
                            ,278 //서리 총탄
                            ,527//매력탄
                            ,533 //매력탄
                            //, //공포탄 총탄
                            ,284// 자동유도 탄환
                            ,352//그림자 탄환
                            ,375 //간편 재장전 총탄
                            ,635 //스노우총탄
                            //,//원격 총탄
                            ,528 //좀비탄
                            //대공포탄
                            ,538// 은탄
                            ,532//도금 총탄
                            ,627//백금 총알
                            ,569//혼돈 탄환
                            //,521// 확률탄
                            ,630//붕총탄
                            ,114//바이오닉 레그
                            ,427//산탄총 커피
                            ,426//산탄 콜라
                            ,212//탄도 장화
                            ,110//매직 스위트
                            ,187//요령 좋은 성격
                            ,435//콧털
                            ,213//리치의 집게손가락
                            ,353//분노의 사진
                            ,115//투표 용지
                            ,414//인간 탄환
                            ,354//군사 훈련
                            ,463//쥐갈공명의 반지
                            ,112//지도 제작자의 반지
                            ,158//구덩이 군주의 부적
                            ,191//화염 저항의 반지
                            ,495//총기옥
                            ,488//상자 집착증 반지
                            ,309//클로런시 반지
                            ,174//상자 우정 반지
                            ,254//상자 우정 반지
                            ,294//흉내내기 우정 반지
                            ,456//방아쇠 반지
                            ,159//건드로메다 질병
                            ,258//브로콜리
                            ,431//발키리 용액
                            ,167//핏빛 눈알
                            ,160//건나이트 투구
                            ,161//건나이트 정강이받이
                            ,162//건나이트 건틀렛
                            ,163//건나이트 갑옷
                            ,219//늙은 기사의 방패
                            ,222//늙은 기사의 투구
                            ,305//오래된 문장
                            ,457//가시 갑옷
                            ,564//풀메탈재킷
                            ,256//무거운 장화
                            ,193//맹독충 장화
                            ,315//건부츠
                            ,526//뿅뿅 부츠
                            //,667//쥐 장화. 엘리베이터 불가
                            ,214//동전 왕관
                            ,165//기름칠한 실린더
                            ,170//아이스큐브
                            ,190//롤링 아이
                            ,135//전쟁의 톱니바퀴
                            ,119//메트로놈
                            ,138//벌집
                            ,137//지도
                            ,281//총굴 청사진
                            ,253//총굴 고추
                            ,259//항체
                            ,262//화이트 구온석
                            ,263//오렌지 구온석
                            ,264//클리어 구온석
                            ,466//그린 구온석
                            ,260//핑크 구온석
                            ,269//레드 구온석
                            ,270//블루 구온석
                            ,565//유리 구온석
                            //핫뜨거 손목시계
                            ,280//드럼 클립
                            ,287//백업용 총
                            ,290//선글라스
                            ,293//흉내내기 이빨 목걸이
                            ,307//왁스 날개
                            ,312//블래스트 투구
                            ,313//몬스터 혈액
                            ,314//나노머신
                            ,289//칠 잎 클로버
                            ,326//이인자
                            ,321//황금 탄환 부적
                            ,325//혼돈 탄환 부적
                            ,322//로드스톤 탄환 부적
                            ,342//우라늄 탄환 부적
                            ,343//구리 탄환 부적
                            ,344//서리 탄환 부적
                            ,396//테이블 기술 - 마음의 눈
                            ,397//테이블 기술 - 자금
                            ,398//테이블 기술 - 로켓
                            //,//테이블 기술 - 산탄총
                            //,321//테이블 기술 - 히트
                            ,399//테이블 기술 - 분노
                            ,400//테이블 기술 - 공포탄
                            ,465//테이블 기술 - 기절
                            ,421//하트 권총집
                            ,423//하트 목걸이
                            ,424//하트 보틀
                            ,425//하트 지갑
                            ,440//루비 팔찌
                            ,409//고장 난 텔레비전
                            ,364//얼음 심장
                            ,255//고대 영웅의 반다나
                            ,436//피묻은 목도리
                            ,437//근이완제
                            ,500//힙 홀스터
                            ,311//복제인간
                            //,452//스펀지
                            ,453//방독면
                            ,454//방호복
                            ,487//상자 해부학
                            ,166//쉘레톤 열쇠
                            ,490//금전 벽돌
                            ,529//전투 깃발
                            ,491//조력자
                            ,492//늑대
                            ,300//개
                            ,249//올빼미
                            ,301//슈퍼 스페이스 터틀
                            ,127//뻥이에요
                            ,148//고물
                            ,641//황금 고물
                            ,580//쓰레기
                            ,318//R2G2
                            ,442//배지
                            ,572//닭피리
                            ,232//우주의 친구
                            ,451//돼지→영웅 돼지
                            ,632//칠면조
                            ,664//어린 흉내쟁이
                            ,461//공포탄 동료의 반지
                            ,493//현금 가방
                            ,494//은하 용맹 훈장
                            ,434//총탄 우상
                            ,271//납 어레미
                            ,570//노란 약실.저주2
                        };
                        */

            PickupObject o;
            for (int i = 0; i < my2.Count; i++)
            {
                try
                {
                    if ((o = PickupObjectDatabase.GetById(my2[i])) != null)
                    {
                        LootEngine.TryGivePrefabToPlayer(o.gameObject, GameManager.Instance.PrimaryPlayer, true);
                    }
                }
                catch (Exception)
                {
                    UnityEngine.Debug.Log("해당 아이템 없음:" + i);
                }
            }
        }

        public static void SetMyItemCurse()
        {
            int[] my2 = new int[]
            {
                443//
                ,821    //스카우터
                ,65
                ,822    //카타나 총탄
                ,579    //공포탄 총탄
                ,571    //저주받은 총탄
                ,276    //스파이스
                ,631    //뻥뻥 성배   Blank Personality   BlankPersonality
                ,285    //핏빛 브로치  Blood Brooch    VampiricArmor
                ,525    //비탄의 상자
                ,407    //여섯 번째 방
                ,166    //쉘레톤 열쇠
                ,439    //브래킷 열쇠
                ,499    //오래된 공포탄
                ,570    //노란 약실

                ,762    //완성된 총
            };


            PickupObject o;
            for (int i = 0; i < my2.Length; i++)
            {
                try
                {
                    if ((o = PickupObjectDatabase.GetById(my2[i])) != null)
                    {
                        LootEngine.TryGivePrefabToPlayer(o.gameObject, GameManager.Instance.PrimaryPlayer, true);
                    }
                }
                catch (Exception)
                {
                    UnityEngine.Debug.Log("해당 아이템 없음:" + i);
                }
            }
        }

        // 아이템 교체시 등 스텟 일괄 갱신되서 초기화됨
        public static void SetHealthMaximum()
        {

            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.healthHaver.SetHealthMaximum(9.0f);
                    playerController.healthHaver.ApplyHealing(9.0f);
                    //playerController.healthHaver.maximumHealth= 99.0f;
                }

            }

            // 디버프
            /*
                    ShrineCost randomCost = new ShrineCost();
                    randomCost.costType = ShrineCost.CostType.HEALTH;
                 randomCost.cost = 9;
                 */

            ShrineBenefit randomBenefit = new ShrineBenefit();
            randomBenefit.benefitType = ShrineBenefit.BenefitType.HEALTH;
            randomBenefit.amount = 9;
            randomBenefit.statMods = new StatModifier[1];
            randomBenefit.statMods[0] = new StatModifier();
            Debug.Log("SetHealthMaximum" + randomBenefit.statMods.Length);
            randomBenefit.statMods[0].statToBoost = PlayerStats.StatType.Health;
            randomBenefit.statMods[0].amount = 9;
            randomBenefit.statMods[0].modifyType = StatModifier.ModifyMethod.ADDITIVE;


            /*            if (randomBenefit.statMods[0].statToBoost == PlayerStats.StatType.Health)
                        {
                            randomBenefit.statMods[0].amount = (float)UnityEngine.Random.Range(1, 3);
                        }
                        elseq if (randomBenefit.statMods[0].statToBoost == PlayerStats.StatType.MovementSpeed)
                        {
                            randomBenefit.statMods[0].amount = UnityEngine.Random.Range(1.5f, 4f);
                        }
                        else if (randomBenefit.statMods[0].statToBoost == PlayerStats.StatType.Damage)
                        {
                            randomBenefit.statMods[0].amount = UnityEngine.Random.Range(1.2f, 1.5f);
                        }
            */
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    randomBenefit.ApplyBenefit(playerController);
                    //randomCost.ApplyCost(playerController);
                }
            }
            //maximumHealth
            /*
                        public enum StatType
                    {
                        // Token: 0x040085DF RID: 34271
                        MovementSpeed,
                        // Token: 0x040085E0 RID: 34272
                        RateOfFire,
                        // Token: 0x040085E1 RID: 34273
                        Accuracy,
                        // Token: 0x040085E2 RID: 34274
                        Health,
                        // Token: 0x040085E3 RID: 34275
                        Coolness,
                        // Token: 0x040085E4 RID: 34276
                        Damage,
                        // Token: 0x040085E5 RID: 34277
                        ProjectileSpeed,
                        // Token: 0x040085E6 RID: 34278
                        AdditionalGunCapacity,
                        // Token: 0x040085E7 RID: 34279
                        AdditionalItemCapacity,
                        // Token: 0x040085E8 RID: 34280
                        AmmoCapacityMultiplier,
                        // Token: 0x040085E9 RID: 34281
                        ReloadSpeed,
                        // Token: 0x040085EA RID: 34282
                        AdditionalShotPiercing,
                        // Token: 0x040085EB RID: 34283
                        KnockbackMultiplier,
                        // Token: 0x040085EC RID: 34284
                        GlobalPriceMultiplier,
                        // Token: 0x040085ED RID: 34285
                        Curse,
                        // Token: 0x040085EE RID: 34286
                        PlayerBulletScale,
                        // Token: 0x040085EF RID: 34287
                        AdditionalClipCapacityMultiplier,
                        // Token: 0x040085F0 RID: 34288
                        AdditionalShotBounces,
                        // Token: 0x040085F1 RID: 34289
                        AdditionalBlanksPerFloor,
                        // Token: 0x040085F2 RID: 34290
                        ShadowBulletChance,
                        // Token: 0x040085F3 RID: 34291
                        ThrownGunDamage,
                        // Token: 0x040085F4 RID: 34292
                        DodgeRollDamage,
                        // Token: 0x040085F5 RID: 34293
                        DamageToBosses,
                        // Token: 0x040085F6 RID: 34294
                        EnemyProjectileSpeedMultiplier,
                        // Token: 0x040085F7 RID: 34295
                        ExtremeShadowBulletChance,
                        // Token: 0x040085F8 RID: 34296
                        ChargeAmountMultiplier,
                        // Token: 0x040085F9 RID: 34297
                        RangeMultiplier,
                        // Token: 0x040085FA RID: 34298
                        DodgeRollDistanceMultiplier,
                        // Token: 0x040085FB RID: 34299
                        DodgeRollSpeedMultiplier,
                        // Token: 0x040085FC RID: 34300
                        TarnisherClipCapacityMultiplier,
                        // Token: 0x040085FD RID: 34301
                        MoneyMultiplierFromEnemies
                    }*/

        }

        public static void SetBlank99()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.Blanks = 99;
                }
            }
        }

        public static void SetArmor99()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.healthHaver.Armor = 99.0f;
                }
            }
        }

        public static void SetMAX_HELD9()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.MAX_ITEMS_HELD = 99;
                    //playerController.MAX_GUNS_HELD = 9;
                }
            }
        }

        // 키
        public static void SetMaxBullets()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.carriedConsumables.KeyBullets = 99;
                    //playerController.MAX_GUNS_HELD = 9;
                }
            }
        }

        // 키
        public static void SetMaxCurrency()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.carriedConsumables.Currency = 999999999;
                    //playerController.MAX_GUNS_HELD = 9;
                }
            }
        }

        // 저주 제거
        public static void SetClearCurse()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[i];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    StatModifier statModifier = new StatModifier();
                    statModifier.amount = -100f;
                    statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                    statModifier.statToBoost = PlayerStats.StatType.Curse;
                    playerController.ownerlessStatModifiers.Add(statModifier);
                    playerController.stats.RecalculateStats(playerController, false, false);
                }
            }
        }

        public static void SetDropPassive(bool all)
        {
            int k;
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController player = GameManager.Instance.AllPlayers[i];
                if (player && player.healthHaver.IsAlive)
                {
                    if (player.passiveItems != null)
                    {
                        if (all)
                        {
                            //player.maxActiveItemsHeld = player.MAX_ITEMS_HELD + (int)player.stats.GetStatValue(PlayerStats.StatType.AdditionalItemCapacity);
                            //while (1 < player.passiveItems.Count)
                            for (k = player.passiveItems.Count; k > 0; k--)
                            {
                                if (player.passiveItems[k - 1].CanBeDropped)
                                    //player.DropActiveItem(player.activeItems[player.activeItems.Count - 1], 4f, false);
                                    player.DropPassiveItem(player.passiveItems[k - 1]);
                            }
                        }
                        else
                        {
                            k = player.passiveItems.Count;
                            if (1 < k)
                                if (player.passiveItems[k - 1].CanBeDropped)
                                    player.DropPassiveItem(player.passiveItems[k - 1]);
                        }

                    }
                }
            }
        }

        public static void SetForceBlank()
        {
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
            {
                PlayerController player = GameManager.Instance.AllPlayers[i];
                if (player && player.healthHaver.IsAlive)
                {
                    player.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                }
            }
        }

    }
}
