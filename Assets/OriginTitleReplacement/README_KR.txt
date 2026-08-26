ORIGIN 타이틀 - Image-Locked Hover FIX

이번 문제의 실제 원인:
Game View가 Free Aspect일 때 배경 이미지는 AspectRatioFitter(EnvelopeParent) 때문에 확대/크롭되는데,
이전 버전의 버튼/Hover는 Canvas 기준 좌표라서 이미지와 서로 다른 비율로 움직였습니다.
그래서 Settings에 마우스를 올려도 파란색이 글씨 아래에 뜨는 식으로 어긋났습니다.

이번 수정:
- 버튼 4개를 FinalTitleBackground 이미지의 자식으로 이동
- 따라서 배경이 확대/크롭될 때 버튼과 Hover도 정확히 같은 비율로 따라감
- 1672×941 원본 이미지에서 메뉴 사각형을 다시 측정
- 선택 Fill Alpha를 0.025~0.055로 매우 낮춤
- 글씨를 가리는 불투명 파란 박스 제거
- 얇은 파란 테두리 + 골드 왼쪽 라인만 강조
- Start Game 기본 파란색은 다른 메뉴 선택 시 14%만 아주 약하게 어둡게 함
- 기존 Start / Continue / Settings / Exit 기능 유지

적용 순서:
1. Assets/OriginTitleReplacement 기존 폴더를 삭제
2. 이 ZIP의 OriginTitleReplacement 폴더 전체를 Assets 바로 아래에 복사
3. Unity 컴파일 완료 기다리기
4. Hub_Field_Lightweight_V2 씬 열기
5. Play OFF
6. Tools > Game UI > ORIGIN UI > 12. Replace Title Final (Image-Locked Hover)
7. Play

이전 9 / 10 / 11 메뉴는 사용하지 마세요.
