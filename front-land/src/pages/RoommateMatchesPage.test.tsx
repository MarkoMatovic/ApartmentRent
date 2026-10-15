import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { RoommateMatchScore } from '../shared/types/analytics';

const api = vi.hoisted(() => ({ getRoommateMatches: vi.fn() }));
const navigate = vi.hoisted(() => vi.fn());

vi.mock('../shared/api/machineLearning', () => ({ machineLearningApi: api }));
vi.mock('../shared/context/AuthContext', () => ({ useAuth: () => ({ user: { userId: 1 } }) }));
vi.mock('react-router-dom', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router-dom')>()),
  useNavigate: () => navigate,
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, unknown>) => {
      const values = options ? Object.entries(options).filter(([name]) => name !== 'defaultValue').map(([, v]) => v).join('|') : '';
      return values ? `${key}:${values}` : key;
    },
  }),
}));

import RoommateMatchesPage from './RoommateMatchesPage';

// Exactly what the server sends (RoommateMatchScoreDto): the percentage is 0-100, the person is nested.
const fromServer = (over: Partial<RoommateMatchScore> = {}): RoommateMatchScore => ({
  roommateId: 12,
  roommate: {
    roommateId: 12, userId: 34, firstName: 'Jovana', lastName: 'Jović',
    profilePicture: 'data:image/png;base64,AAAA', profession: 'Dizajner', preferredLocation: 'Beograd',
  },
  matchPercentage: 72.4,
  featureScores: { Budget: 100, Lifestyle: 60.2, Preferences: 75, Location: 50 },
  matchQuality: 'Good',
  ...over,
});

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter><RoommateMatchesPage /></MemoryRouter>
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.clearAllMocks();
  api.getRoommateMatches.mockResolvedValue([fromServer()]);
});

describe('RoommateMatchesPage', () => {
  it('shows the percentage the server computed, never NaN', async () => {
    renderPage();

    expect((await screen.findAllByText('72%')).length).toBeGreaterThan(0);
    expect(screen.queryByText(/NaN/)).not.toBeInTheDocument();
  });

  it('shows the person, with their picture', async () => {
    renderPage();

    expect(await screen.findByText('Jovana')).toBeInTheDocument();
    const photo = screen.getByRole('img', { name: 'Jovana' });
    expect(photo).toHaveAttribute('src', 'data:image/png;base64,AAAA');
  });

  it('falls back to the initial when the person has no picture', async () => {
    api.getRoommateMatches.mockResolvedValue([fromServer({ roommate: { roommateId: 12, userId: 34, firstName: 'Jovana' } })]);

    renderPage();

    expect(await screen.findByText('J')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('names each part of the score, in the language of the user', async () => {
    renderPage();

    await screen.findByText('Jovana');
    expect(screen.getByText('roommates:lifestyleFactor:60')).toBeInTheDocument();
    expect(screen.getByText('roommates:budgetFactor:100')).toBeInTheDocument();
    expect(screen.getByText('roommates:preferencesFactor:75')).toBeInTheDocument();
    expect(screen.getByText('roommates:locationFactor:50')).toBeInTheDocument();
  });

  it('opens the roommate profile by its id, and the chat with that person', async () => {
    renderPage();
    await screen.findByText('Jovana');

    fireEvent.click(screen.getByText('common:view'));
    expect(navigate).toHaveBeenCalledWith('/roommates/12');

    fireEvent.click(screen.getByText('roommates:contact'));
    expect(navigate).toHaveBeenCalledWith('/chat?userId=34');
  });

  it('gives the count to the plural text and uses a translated refresh button', async () => {
    api.getRoommateMatches.mockResolvedValue([fromServer(), fromServer({ roommateId: 13, matchPercentage: 41 })]);

    renderPage();

    expect(await screen.findByText('roommates:matchesFound:2')).toBeInTheDocument();
    fireEvent.click(screen.getByText('roommates:refreshMatches'));
    await waitFor(() => expect(api.getRoommateMatches).toHaveBeenCalledTimes(2));
  });

  it('colours the score by how good the match is', async () => {
    api.getRoommateMatches.mockResolvedValue([
      fromServer({ roommateId: 1, matchPercentage: 85 }),
      fromServer({ roommateId: 2, matchPercentage: 65 }),
      fromServer({ roommateId: 3, matchPercentage: 45 }),
      fromServer({ roommateId: 4, matchPercentage: 20 }),
    ]);

    renderPage();
    await screen.findByText('85%', { selector: '.MuiChip-label' });

    const colour = (percent: string) => screen.getByText(percent, { selector: '.MuiChip-label' }).closest('.MuiChip-root')!.className;
    expect(colour('85%')).toContain('MuiChip-colorSuccess');
    expect(colour('65%')).toContain('MuiChip-colorPrimary');
    expect(colour('45%')).toContain('MuiChip-colorWarning');
    expect(colour('20%')).toContain('MuiChip-colorError');
  });

  it('says so when there are no matches', async () => {
    api.getRoommateMatches.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('roommates:noMatches')).toBeInTheDocument();
  });
});
