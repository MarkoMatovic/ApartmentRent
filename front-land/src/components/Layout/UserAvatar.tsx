import React from 'react';
import { Avatar } from '@mui/material';
import type { SxProps, Theme } from '@mui/material';

interface UserAvatarProps {
  firstName?: string;
  lastName?: string;
  src?: string;
  size?: number;
  sx?: SxProps<Theme>;
}

/** The user's photo, or their initials when there is none. */
const UserAvatar: React.FC<UserAvatarProps> = ({ firstName, lastName, src, size = 32, sx }) => {
  const initials = `${firstName?.[0] ?? ''}${lastName?.[0] ?? ''}`.toUpperCase();

  return (
    <Avatar
      src={src || undefined}
      alt={`${firstName ?? ''} ${lastName ?? ''}`.trim()}
      sx={[{ width: size, height: size, fontSize: size * 0.42 }, ...(Array.isArray(sx) ? sx : [sx])]}
    >
      {initials}
    </Avatar>
  );
};

export default UserAvatar;
