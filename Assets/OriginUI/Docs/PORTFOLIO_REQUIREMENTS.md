# ORIGIN UI - 과제 요구사항 대응표

| 과제 요구사항 | ORIGIN UI V1.2 대응 | 상태 |
|---|---|---|
| UI/UX 설계 문서 | UIUX_DESIGN_GUIDE.md | 구현됨 |
| 화면 목록/전환 흐름 | Title -> HUD -> Pause/Result/GameOver | 구현됨 |
| 기준 해상도/Canvas Scaler | 1920x1080, Scale With Screen Size, Match 0.5 | 구현됨 |
| UI 리소스 규칙/콘셉트 | 디자인 가이드 1/7/8/9절 | 구현됨 |
| GUI 디자인 가이드 | 최소 버튼/글자/대비/우선순위/입력 표 | 구현됨 |
| 구현 가능성 검토 | DungeonInfo/SCORE 설계 변경 기록 | 구현됨 |
| HUD 3개 이상 | HP + Objective + TIME (추가 HUNT) | 구현됨 |
| 실제 스크립트 변수 연결 | OriginUIState -> OriginHUDController | 구현됨 |
| 메뉴 3개 이상 | Title + HUD + Pause + Result + GameOver | 구현됨 |
| 버튼 화면 전환 | OriginUIFlowController | 구현됨 |
| Mouse 클릭 | UGUI Button + GraphicRaycaster | 구현됨, 수동 확인 필요 |
| Keyboard 메뉴 이동 | InputSystemUIInputModule | 구현됨, 수동 확인 필요 |
| Submit/Cancel | Enter / Esc | 구현됨, 수동 확인 필요 |
| 재사용 Prefab 2개 이상 | MenuButton / GaugeBar / ToastMessage | 구현됨 |
| UI 피드백 2개 이상 | Button 상태/스케일 + Toast Fade | 구현됨 |
| 자체 체크리스트 | UIUX_CHECKLIST.md | 제공됨, 실제 결과 작성 필요 |
| UI 2개 수정/재확인 기록 | 체크리스트 템플릿 제공 | 직접 테스트 결과 작성 필요 |
| 실행 화면 캡처 2장 이상 | HUD / 메뉴 화면 권장 | 직접 캡처 필요 |

## 중요한 점
코드와 구조만으로 과제 제출이 100% 끝나는 것은 아니다. 과제에서 요구하는 '실제 결과', '수정 내용', '재확인 결과', '실행 화면 캡처'는 프로젝트를 직접 실행한 결과여야 하므로 제출 전에 본인이 테스트하고 기록해야 한다.
