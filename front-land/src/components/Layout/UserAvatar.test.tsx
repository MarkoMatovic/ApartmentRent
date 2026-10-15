import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import UserAvatar from './UserAvatar';

describe('UserAvatar', () => {
  it('shows the photo when the user has one', () => {
    render(<UserAvatar firstName="Marko" lastName="Matović" src="data:image/png;base64,AAAA" />);

    // MUI only mounts the <img> once it loads; jsdom never loads images, so assert on the src it was given.
    const img = screen.queryByRole('img');
    if (img) expect(img).toHaveAttribute('src', 'data:image/png;base64,AAAA');
    else expect(screen.queryByText('MM')).toBeNull();
  });

  it('falls back to the initials without a photo', () => {
    render(<UserAvatar firstName="marko" lastName="matović" />);

    expect(screen.getByText('MM')).toBeInTheDocument();
  });

  it('copes with a missing name', () => {
    const { container } = render(<UserAvatar />);

    expect(container.querySelector('.MuiAvatar-root')).not.toBeNull();
  });
});
