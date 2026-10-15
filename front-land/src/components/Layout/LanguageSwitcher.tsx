import React, { useState } from 'react';
import { alpha, IconButton, Menu, MenuItem } from '@mui/material';
import { Check as CheckIcon, Language as LanguageIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';

const languages = [
  { code: 'sr', label: '🇷🇸 SR' },
  { code: 'en', label: '🇬🇧 EN' },
  { code: 'ru', label: '🇷🇺 RU' },
  { code: 'de', label: '🇩🇪 DE' },
];

const LanguageSwitcher: React.FC = () => {
  const { i18n } = useTranslation();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // The browser language detector can store "en-US" or "sr-RS"; compare the base language so the
  // current one is always highlighted (it used to match only an exact "en" / "sr").
  const current = (i18n.resolvedLanguage ?? i18n.language ?? 'sr').split('-')[0];

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleLanguageChange = (langCode: string) => {
    i18n.changeLanguage(langCode);
    handleClose();
  };

  return (
    <>
      <IconButton color="inherit" onClick={handleClick}>
        <LanguageIcon />
      </IconButton>
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleClose}>
        {languages.map((lang) => {
          const isCurrent = current === lang.code;
          return (
            <MenuItem
              key={lang.code}
              onClick={() => handleLanguageChange(lang.code)}
              selected={isCurrent}
              aria-current={isCurrent ? 'true' : undefined}
              sx={(theme) => ({
                minWidth: 150,
                borderLeft: '3px solid transparent',
                '&.Mui-selected': {
                  fontWeight: 700,
                  borderLeftColor: theme.palette.primary.main,
                  bgcolor: alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.3 : 0.16),
                  '&:hover': {
                    bgcolor: alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.4 : 0.24),
                  },
                },
              })}
            >
              {lang.label}
              {isCurrent && <CheckIcon fontSize="small" sx={{ ml: 'auto', pl: 1, color: 'primary.main' }} />}
            </MenuItem>
          );
        })}
      </Menu>
    </>
  );
};

export default LanguageSwitcher;
