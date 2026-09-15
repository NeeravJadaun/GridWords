import { test, expect, type Page } from '@playwright/test'

// Common short words, verified present in the bundled dictionary
// (src/WordDuel.Domain/WordList/words.txt). Match setup draws a
// deterministic-but-unpredictable rack, so we scan for whichever of these
// the drawn rack can actually play, rather than hard-coding one word.
const CANDIDATE_WORDS = [
  'AT', 'AN', 'AS', 'SO', 'NO', 'OR', 'IT', 'IS', 'IN', 'ON', 'TO', 'BE',
  'SEA', 'ATE', 'EAT', 'TEA', 'ARE', 'EAR', 'ERA', 'ART', 'RAT', 'TAR',
  'SAT', 'SIR', 'AIR', 'ANT', 'TAN', 'RAN', 'TEN', 'NET', 'SET', 'RID',
  'RED', 'DIE', 'TIE', 'LIE', 'LID', 'DID', 'AID', 'AIM', 'SIT', 'SAD',
  'SOLE', 'TALE', 'RATE', 'LATE', 'DATE', 'DEAL', 'LEAD', 'READ', 'TEAR',
  'STAR', 'SEAT', 'EASE', 'IDEA', 'TIDE', 'SIDE',
]

function findPlayableWord(rack: string): string {
  const pool = (letters: string) => {
    const counts = new Map<string, number>()
    for (const ch of letters) counts.set(ch, (counts.get(ch) ?? 0) + 1)
    return counts
  }

  for (const word of CANDIDATE_WORDS) {
    const counts = pool(rack)
    let ok = true
    for (const ch of word) {
      const remaining = counts.get(ch) ?? 0
      if (remaining === 0) {
        ok = false
        break
      }
      counts.set(ch, remaining - 1)
    }
    if (ok) return word
  }

  throw new Error(`No playable candidate word found for rack "${rack}"`)
}

async function createMatch(page: Page, displayName: string) {
  await page.goto('/')
  await page.getByRole('button', { name: 'Create match' }).first().click()
  await page.getByLabel('Display name').fill(displayName)
  await page.getByRole('button', { name: 'Create match' }).last().click()
  await expect(page.locator('.match-id code')).toBeVisible()
  const matchId = (await page.locator('.match-id code').textContent())!.trim()
  return matchId
}

async function joinMatch(page: Page, matchId: string, displayName: string) {
  await page.goto('/')
  await page.getByRole('button', { name: 'Join match' }).first().click()
  await page.getByLabel('Display name').fill(displayName)
  await page.getByLabel('Match ID').fill(matchId)
  await page.getByRole('button', { name: 'Join match' }).last().click()
  await expect(page.locator('.match-id code')).toBeVisible()
}

async function readRack(page: Page): Promise<string> {
  const letters = await page.locator('.rack-tile .letter').allTextContents()
  return letters.join('')
}

test('two players can play a full match: create, join, move, score, pass to finish', async ({ browser }) => {
  const contextA = await browser.newContext()
  const contextB = await browser.newContext()
  const pageA = await contextA.newPage()
  const pageB = await contextB.newPage()

  // Player A creates the match.
  const matchId = await createMatch(pageA, 'Ada')
  await expect(pageA.locator('.waiting-banner')).toBeVisible()

  // Player B joins using the shared match ID.
  await joinMatch(pageB, matchId, 'Grace')

  // Both players should now see the match in progress.
  await expect(pageA.locator('.waiting-banner')).toBeHidden({ timeout: 10_000 })
  await expect(pageA.getByText('Grace')).toBeVisible()
  await expect(pageB.getByText('Ada')).toBeVisible()

  // Player A (seat 0) always moves first — find a word their rack can play.
  const rackA = await readRack(pageA)
  const word = findPlayableWord(rackA)

  // Click the center board cell (row 4, col 4 in a 1-indexed 7x7 board) to
  // anchor the placement, then type the word and submit.
  await pageA.getByRole('button', { name: /Row 4, column 4/ }).click();
  await pageA.getByLabel('Tiles to place').fill(word)
  await pageA.getByRole('button', { name: 'Submit move' }).click()

  // The move should score and hand the turn to Player B.
  await expect(pageA.locator('.move-summary-banner')).toBeVisible({ timeout: 10_000 })

  // Player B should see the update arrive in real time via SignalR, without
  // needing to manually refresh — this is the live board sync in action.
  await expect(pageB.locator('.board-cell.filled').first()).toBeVisible({ timeout: 10_000 })

  // Both players pass, and a third pass ends the match (3 consecutive passes).
  await pageB.getByRole('button', { name: 'Pass' }).click()
  await expect(pageA.getByRole('button', { name: 'Pass' })).toBeEnabled({ timeout: 10_000 })
  await pageA.getByRole('button', { name: 'Pass' }).click()
  await expect(pageB.getByRole('button', { name: 'Pass' })).toBeEnabled({ timeout: 10_000 })
  await pageB.getByRole('button', { name: 'Pass' }).click()

  // Match should now show the finished-match summary on both screens.
  await expect(pageA.locator('.match-summary')).toBeVisible({ timeout: 10_000 })
  await expect(pageB.locator('.match-summary')).toBeVisible({ timeout: 10_000 })
  await expect(pageA.locator('.match-summary')).toContainText('wins')

  await contextA.close()
  await contextB.close()
})
