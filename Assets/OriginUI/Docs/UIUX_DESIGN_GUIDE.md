# ORIGIN UI/UX 설계 가이드 V1.2

## 1. UI/UX 콘셉트
ORIGIN의 HUD는 액션 중 화면을 가리지 않는 저간섭 배치를 기본으로 한다. 원신처럼 주요 HUD를 화면 외곽에 두고, 젠레스 존 제로처럼 각진 프레임과 강한 포인트를 사용하며, 몬스터 헌터처럼 사냥에 필요한 시간/처치 진행 정보를 짧고 명확하게 전달한다. 특정 게임의 UI를 그대로 복제하지 않고 ORIGIN의 어두운 금속 패널, 아이보리 선, 골드/핑크/청록 포인트로 재구성한다.

## 2. 기준 환경
- Unity 6
- UGUI + TextMeshPro
- Input System
- Reference Resolution: 1920 x 1080
- Canvas Scaler: Scale With Screen Size
- Match Width Or Height: 0.5

## 3. 화면 목록 / 전환 흐름
- TitleScreen -> START -> HUD
- HUD -> ESC -> PauseMenu -> CONTINUE -> HUD
- HUD -> 클리어 -> ResultMenu -> RETRY 또는 TitleScreen
- HUD -> 실패 -> GameOverMenu -> RETRY 또는 TitleScreen

구현 화면은 Title / HUD / Pause / Result / GameOver 총 5종이다.

## 4. 씬별 HUD 규칙
### Hub_Field_Lightweight_V2
- Player HP: 표시
- Current Objective: 표시
- DungeonInfo(TIME/HUNT): 숨김
- InteractionPrompt: NPC/포탈 근처에서만 표시

### NGF_CompactDungeon
- Player HP: 표시
- Current Objective: 표시
- DungeonInfo(TIME/HUNT): 자동 표시
- TIME: 기본 03:00부터 감소
- HUNT: 기본 0/5부터 증가

ORIGIN_UI는 플레이 중 DontDestroyOnLoad로 유지되며 씬이 바뀌면 `OriginUISceneContext`가 HUD 상태를 자동 변경한다.

## 5. HUD 정보 우선순위
1. HP: 생존 상태이므로 항상 확인 가능해야 한다.
2. 현재 목표: 플레이어가 다음 행동을 잃지 않게 한다.
3. 남은 시간 / 사냥 진행: 던전에서만 표시한다.
4. 조작 가이드: 초반 학습용이며 화면 오른쪽 아래에 둔다.
5. Toast: 목표 갱신/중요 이벤트 때만 짧게 노출한다.

## 6. 입력 방식
- Mouse: 버튼 클릭
- Keyboard: EventSystem + Input System UI Module을 통한 메뉴 이동
- Enter: Submit
- Esc: Cancel / Pause
- 실제 입력 동작은 제출 전 수동 테스트 후 체크리스트에 기록한다.

## 7. 재사용 UI Prefab
- `MenuButton.prefab`: Title/Pause/Result/GameOver 메뉴 버튼
- `GaugeBar.prefab`: Player HP 및 추후 Boss HP/Groggy Gauge
- `ToastMessage.prefab`: 목표 갱신/아이템 획득/패링 성공 알림

## 8. UI 피드백
- Button: Normal / Hover / Pressed / Disabled 색 변화
- Button: Hover/Pressed 시 미세 스케일 변화
- Toast: Fade In / Hold / Fade Out
- HP: 숫자와 게이지가 동일 데이터에 따라 즉시 갱신

## 9. GUI 디자인 가이드 / 구현 가능성
| 항목 | 설계 기준 | 구현 방식 |
|---|---|---|
| 최소 메뉴 버튼 | 340 x 58 px | UGUI Button Prefab |
| 일반 HUD 글자 | 17 px 이상 | TextMeshPro |
| 중요 목표 글자 | 24 px | TextMeshPro Bold |
| 대비 | 어두운 패널 + 밝은 텍스트 | 패널 Sprite + TMP Color |
| 정보 우선순위 | HP > 목표 > 던전정보 > 조작 | 화면 영역 분리 |
| 해상도 대응 | 1920x1080 기준, 비율 변화 대응 | Canvas Scaler + Anchor |
| Mouse | 클릭 | Button + GraphicRaycaster |
| Keyboard | 이동/Submit/Cancel | EventSystem + Input System UI Module |

## 10. 구현 가능성 검토 및 설계 변경 기록
### 변경 1: 던전 정보의 허브 노출
- 원안: TIME/HUNT를 항상 표시
- 문제: 허브에서 사냥 정보가 노출되어 현재 상황과 맞지 않음
- 변경: `NGF_CompactDungeon`에서만 TIME/HUNT 활성화
- 재확인: Hub와 Dungeon을 각각 실행/이동한 뒤 체크리스트에 실제 결과 기록

### 변경 2: SCORE 제거
- 원안: TIME / HUNT / SCORE 표시
- 문제: ORIGIN은 점수 경쟁형 게임이 아니므로 불필요한 정보가 HUD 우선순위를 해침
- 변경: SCORE 제거, TIME/HUNT만 유지
- 재확인: 던전 HUD의 가독성을 체크리스트에 기록

## 11. 실제 게임 시스템 연결 API
- HP: `OriginUIState.SetHP(current, max)`
- Quest: `OriginUIState.SetObjective(text, distance)`
- Enemy: `OriginUIState.SetEnemyProgress(defeated, total)`
- Interaction: `OriginUIState.SetInteraction(visible, text, key)`
- Toast: `OriginToastController.Show(message)`

현재 F6~F10 Demo Key는 UI 동작 검증용이며, Enemy/Quest/HP 실제 시스템이 완성되면 위 API를 기존 게임 코드에서 호출한다.
