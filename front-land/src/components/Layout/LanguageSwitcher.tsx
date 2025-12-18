import React, { useState } from 'react';
import { IconButton, Menu, MenuItem } from '@mui/material';
import { Language as LanguageIcon } from '@mui/icons-material';
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
        {languages.map((lang) => (
          <MenuItem
            key={lang.code}
            onClick={() => handleLanguageChange(lang.code)}
            selected={i18n.language === lang.code}
          >
            {lang.label}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
};

export default LanguageSwitcher;

