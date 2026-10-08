# 자석 아이템 모델 출처

- **이 프로젝트에서 직접 만든 모델**(2026-10-08, 1.0.3). 외부 에셋이 아니므로 별도 라이선스 조건이 없다.
- `Magnet.asset`: 말굽(U자) 자석을 코드로 생성한 메시. 원 단면(반지름 0.085 m) 튜브를 직선 다리 2개(길이 0.22 m) + 반원(반지름 0.2 m) 경로로 훑어 만들고, 양 끝 0.12 m는 은색 서브메시, 나머지는 빨강 서브메시. 땅에 눕힌 방향(XZ 평면), 크기 약 0.57 × 0.17 × 0.51 m. 프리팹 `Assets/Prefabs/MagnetPickup.prefab`에서 X축으로 -25° 기울여 쓴다.
- 재질: `Assets/Game/Materials/Magnet_Red.mat`, `Magnet_Silver.mat`(URP Lit 단색, 텍스처 없음).
- 후보였던 OpenGameArt "Magnet"(Savino, CC0)은 받지 않았다. 화면에서 보고 화풍이 맞지 않으면 그때 교체한다.
