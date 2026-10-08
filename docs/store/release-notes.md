# Urban Survival 출시 노트(업데이트 내역)

- Play Console "출시 노트" 칸에 그대로 붙여 넣는 문안을 버전별로 쌓는다. 언어마다 **500자 이하**(Play 제한).
- 새 버전을 올릴 때는 맨 위에 새 절을 추가한다. `versionName`/`versionCode`는 `ProjectSettings`(Player Settings)와 같아야 하고, `versionCode`는 올릴 때마다 1 이상 커져야 한다.
- 문안에는 실제로 들어간 기능만 적는다. 근거는 `store-listing-draft.md` 1장과 커밋 기록.
- Play Console 입력 형식: `<ko-KR>` … `</ko-KR>`, `<en-US>` … `</en-US>` 태그로 언어를 나눈다.

---

## 1.0.4 (versionCode 5) — 후보 (미정)

- 상태: **후보만.** 사용자가 고른 항목만 넣는다(`patch-plan.md` 1.0.4).

---

## 1.0.3 (versionCode 4) — 새 아이템: 자석 (설계 전)

- 상태: **설계 필요**(지속 시간·드랍 확률·HUD 표시·중첩 규칙). 문안은 구현 뒤 확정.

```
<ko-KR>
(초안) 새 아이템 "자석"이 추가되었습니다. 획득하면 일정 시간 동안 넓은 범위(10 m)의 아이템을 끌어당깁니다.
</ko-KR>
```

---

## 1.0.2 (versionCode 3) — 자원 정리·안정성 (준비 중)

- 상태: **미착수.** 남은 것: Health Circle 자체 도형 교체, Heart 처리(사용자 결정), 크레딧 표기(선택). EmojiOne 제거(`918b160`)는 1.0.1에 함께 들어가므로 이 절에서 뺐다.

```
<ko-KR>
(초안) 화면 자원을 정리하고 안정성을 개선했습니다.
</ko-KR>
```

---

## 1.0.1 (versionCode 2) — 아이템 습득 편의 (빌드 완료, 업로드 대기)

- 상태: **AAB 빌드 완료(2026-10-08 10:39), Play Console 미업로드.** 업로드는 사용자가 나중에 버전 순서대로 한다. 파일 `Builds/Android/UrbanSurvival-1.0.1-2.aab`(80,078,271 B) + `UrbanSurvival-1.0.1-2-1.0.1-v2-IL2CPP.symbols.zip`(26,990,499 B), 권한 0개(문자열 검색 기준). 이력은 `Builds/Android/빌드_이력.md`(Git 밖).
- 기준 커밋: `SideProject-Mobile` `c3dbbf8`(작업 트리의 Unity 자동 재저장 변경이 남은 상태로 빌드).
- 들어간 것: 아이템 자석 연출 `23fb6ea`, 자석 비행 중 디스폰 방어·모바일 인트로 PC 조작 안내 숨김·디버그 기호 설정 `9a0e8a4`, EmojiOne 제거 `918b160`, 버전 `c3dbbf8`. 검증: EditMode 88/88, 에디터 플레이(PC·모바일 강제). 미검증: 실제 기기, 업로드 수락.
- 출시 노트에서 뺀 것: 디스폰 방어(같은 버전에 새로 들어간 자석 연출의 내부 보완), EmojiOne 제거(화면 변화 없음). 이전 초안의 "안정성을 개선했습니다"는 1.0.0 대비 실제 안정성 수정이 없어 뺐다.
- 영어 스토어 등록정보를 추가한 뒤에는 `<en-US>`도 함께 넣는다(등록하지 않은 언어 태그는 오류가 날 수 있음).

```
<ko-KR>
- 가까이 있는 아이템이 캐릭터 쪽으로 끌려와 자동으로 습득됩니다.
- 모바일 캐릭터 선택 화면에 보이던 PC 조작 안내(키보드·마우스)를 숨겼습니다.
</ko-KR>
```

---

## 1.0.0 (versionCode 1) — 첫 출시

- 상태: **비공개 테스트까지 진행 완료(2026-10-08 사용자 보고).** 2026-10-07 내부 테스트에 AAB 업로드 → 비공개 테스트 출시. 검토 결과·테스터 수·테스트 시작일은 Play Console에서 확인해 여기에 적는다.
- 이전 기록: **2026-10-07 내부 테스트 트랙에 AAB 업로드(출시명 1 (1.0.0), ko-KR 출시 노트)**. `Builds/Android/UrbanSurvival-1.0.0-1.aab`(80.1 MB, 사용자 업로드 키 서명, 권한 0개, 2026-10-07 11:23). 새 효과음은 릴리스 빌드에서 청취 확인(사용자).
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
