# Urban Survival 출시 노트(업데이트 내역)

- Play Console "출시 노트" 칸에 그대로 붙여 넣는 문안을 버전별로 쌓는다. 언어마다 **500자 이하**(Play 제한).
- 새 버전을 올릴 때는 맨 위에 새 절을 추가한다. `versionName`/`versionCode`는 `ProjectSettings`(Player Settings)와 같아야 하고, `versionCode`는 올릴 때마다 1 이상 커져야 한다.
- 문안에는 실제로 들어간 기능만 적는다. 근거는 `store-listing-draft.md` 1장과 커밋 기록.
- Play Console 입력 형식: `<ko-KR>` … `</ko-KR>`, `<en-US>` … `</en-US>` 태그로 언어를 나눈다.

---

## 1.0.0 (versionCode 1) — 첫 출시 (초안, 2026-10-07)

- 상태: **AAB 준비됨, 업로드 전**. `Builds/Android/UrbanSurvival-1.0.0-1.aab`(80.1 MB, 사용자 업로드 키 서명, 권한 0개, 2026-10-07 11:23). 새 효과음은 릴리스 빌드에서 청취 확인(사용자).
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
