# Urban Survival 출시 노트(업데이트 내역)

- Play Console "출시 노트" 칸에 그대로 붙여 넣는 문안을 버전별로 쌓는다. 언어마다 **500자 이하**(Play 제한).
- 새 버전을 올릴 때는 맨 위에 새 절을 추가한다. `versionName`/`versionCode`는 `ProjectSettings`(Player Settings)와 같아야 하고, `versionCode`는 올릴 때마다 1 이상 커져야 한다.
- 문안에는 실제로 들어간 기능만 적는다. 근거는 `store-listing-draft.md` 1장과 커밋 기록.
- Play Console 입력 형식: `<ko-KR>` … `</ko-KR>`, `<en-US>` … `</en-US>` 태그로 언어를 나눈다.

---

## 1.0.1 (versionCode 2) — 다음 업데이트 (준비 중, 내용 미확정)

- 상태: **후보 정리만 됨.** 실제로 들어간 변경만 아래 문안에 남기고 나머지는 지운다. 후보 목록과 담당은 `WorkNotes/20261007_이어서_작업_프롬프트.md` 2장.
- 후보: 모바일 캐릭터 선택 화면의 PC 조작 안내 수정, 아이템 자석 연출(3 m), 자석 아이템(10 m, 시간제 버프), 출처 미확정 자원 교체, 네이티브 디버그 기호 업로드, 영어 스토어 등록.
- 영어 스토어 등록정보를 추가한 뒤에는 `<en-US>`도 함께 넣는다(등록하지 않은 언어 태그는 오류가 날 수 있음).

```
<ko-KR>
(작성 예정 — 실제 반영된 변경만)
- 모바일 캐릭터 선택 화면 안내 문구 수정
- 아이템이 가까이 오면 캐릭터 쪽으로 끌려와 습득됩니다
</ko-KR>
```

---

## 1.0.0 (versionCode 1) — 첫 출시 (초안, 2026-10-07)

- 상태: **2026-10-07 내부 테스트 트랙에 AAB 업로드(출시명 1 (1.0.0), ko-KR 출시 노트)**. `Builds/Android/UrbanSurvival-1.0.0-1.aab`(80.1 MB, 사용자 업로드 키 서명, 권한 0개, 2026-10-07 11:23). 새 효과음은 릴리스 빌드에서 청취 확인(사용자).
- 기준 커밋: `SideProject-Mobile` `ab09266`(코드·에셋 기준. 그 뒤 `1aeca8a`는 문서만 바뀜).

```
<ko-KR>
Urban Survival 첫 출시입니다.
- 쿼터뷰 좀비 생존 슈팅: 30분을 버티고 30분 보스를 쓰러뜨리면 무한 모드에 도전할 수 있습니다.
- 캐릭터 2종, 무기 4종(권총·소총·SMG·산탄총). 적을 쓰러뜨려 무기·탄약·회복 상자를 얻으세요.
- 10분마다 보스 등장, 보스 연습(PRACTICE) 모드 제공.
- 모바일 조작: 이동 스틱 + 자동 조준 FIRE 또는 쌍둥이 스틱 직접 조준(설정에서 변경).
- 광고·인앱 결제·온라인 기능이 없습니다.
</ko-KR>
<en-US>
First release of Urban Survival.
- Top-down zombie survival shooter: survive 30 minutes, defeat the 30-minute boss, then take on Endless mode.
- 2 characters and 4 weapons (pistol, rifle, SMG, shotgun). Defeat zombies to collect weapons, ammo and health packs.
- A boss appears every 10 minutes. Boss PRACTICE mode included.
- Mobile controls: move stick with auto-aim FIRE, or twin-stick manual aim (change in Settings).
- No ads, no in-app purchases, no online features.
</en-US>
```

- 글자 수(공백 포함, 태그 제외): 한국어 258자, 영어 472자(500자 이하, 2026-10-07 계산).
- 업로드 전에 확인할 것: Unity Analytics를 끈 뒤 릴리스 빌드에 `INTERNET` 권한이 남아 있어도 "온라인 기능 없음"은 사실이다(게임 코드에 네트워크 통신 없음). 다만 Data Safety 답변은 `store-listing-draft.md` 6장을 따른다.
