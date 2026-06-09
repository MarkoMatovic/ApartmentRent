import React, { useEffect, useRef, useState } from 'react';
import {
  Container, Typography, Box, Grid, Paper, Button, Avatar, Chip,
  Card, CardContent, Dialog, DialogTitle, DialogContent, DialogActions,
  Divider, LinearProgress, Tooltip,
} from '@mui/material';
import {
  LocationOn as LocationIcon, Euro as EuroIcon, CalendarToday as CalIcon,
  Edit as EditIcon, Delete as DeleteIcon, Person as PersonIcon,
  SmokingRooms as SmokingIcon, SmokeFree as NoSmokingIcon,
  Pets as PetsIcon, MusicNote as MusicIcon, CheckCircle as CheckIcon,
  Cancel as CancelIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { roommatesApi } from '../shared/api/roommates';
import { analyticsApi } from '../shared/api/analytics';
import { useAuth } from '../shared/context/AuthContext';
import {
  getAge, formatAvailableFrom,
  LIFESTYLE_ICONS, LIFESTYLE_LABELS,
  SCHEDULE_ICONS, SCHEDULE_LABELS,
  CLEANLINESS_LABELS, GENDER_LABELS,
} from '../shared/types/roommate';

// Preference row component
const PrefRow: React.FC<{ label: string; value?: boolean | string | null; trueLabel?: string; falseLabel?: string }> = ({
  label, value, trueLabel = 'Da', falseLabel = 'Ne',
}) => {
  if (value === undefined || value === null || value === '') return null;
  const isTrue = value === true || value === 'true';
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', py: 0.8 }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
        {typeof value === 'boolean' ? (
          isTrue
            ? <><CheckIcon sx={{ fontSize: 16, color: 'success.main' }} /><Typography variant="body2" color="success.main" fontWeight="medium">{trueLabel}</Typography></>
            : <><CancelIcon sx={{ fontSize: 16, color: 'text.disabled' }} /><Typography variant="body2" color="text.disabled">{falseLabel}</Typography></>
        ) : (
          <Typography variant="body2" fontWeight="medium">{value}</Typography>
        )}
      </Box>
    </Box>
  );
};

const RoommateDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [deleteOpen, setDeleteOpen] = useState(false);

  const { data: roommate, isLoading } = useQuery({
    queryKey: ['roommate', id],
    queryFn: () => roommatesApi.getById(Number(id)).catch(() => roommatesApi.getByUserId(Number(id))),
    enabled: !!id,
  });

  const deleteMutation = useMutation({
    mutationFn: () => roommatesApi.delete(roommate!.roommateId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roommates'] });
      queryClient.invalidateQueries({ queryKey: ['roommate-my'] });
      navigate('/roommates');
    },
  });

  const viewTracked = useRef<string | null>(null);
  useEffect(() => {
    if (roommate && id && viewTracked.current !== id) {
      viewTracked.current = id;
      analyticsApi.trackEvent('RoommateView', 'Roommates', roommate.roommateId, 'Roommate');
    }
  }, [roommate, id]);

  if (isLoading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><Typography>Učitava se...</Typography></Box>;
  if (!roommate) return <Box sx={{ textAlign: 'center', py: 8 }}><Typography>Profil nije pronađen.</Typography></Box>;

  const age = getAge(roommate.dateOfBirth);
  const isOwn = user?.userId === roommate.userId;
  const availableFrom = formatAvailableFrom(roommate.availableFrom);

  // Profile completeness (same logic as card)
  const compFields = [roommate.bio, roommate.profession, roommate.hobbies, roommate.preferredLocation,
    roommate.languages, roommate.lifestyle, roommate.cleanliness, roommate.budgetMin,
    roommate.availableFrom, roommate.gender !== undefined && roommate.gender !== 0,
    roommate.workSchedule !== undefined, roommate.profilePicture];
  const completeness = Math.round((compFields.filter(Boolean).length / compFields.length) * 100);

  const hobbies = roommate.hobbies?.split(',').map(h => h.trim()).filter(Boolean) || [];
  const languages = roommate.languages?.split(',').map(l => l.trim()).filter(Boolean) || [];

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Grid container spacing={3}>
        {/* ── Left column ─────────────────────────────────────────────── */}
        <Grid item xs={12} md={8}>

          {/* Hero header */}
          <Paper sx={{ borderRadius: 3, overflow: 'hidden', mb: 3 }}>
            <Box sx={{
              background: 'linear-gradient(135deg, #1C3C58 0%, #305B7A 60%, #4B7795 100%)',
              px: 3, pt: 4, pb: 3,
            }}>
              <Box sx={{ display: 'flex', gap: 3, alignItems: 'flex-end' }}>
                <Avatar src={roommate.profilePicture} sx={{ width: 100, height: 100, border: '4px solid rgba(255,255,255,0.3)', flexShrink: 0 }}>
                  <PersonIcon sx={{ fontSize: 50 }} />
                </Avatar>
                <Box sx={{ flex: 1, pb: 0.5 }}>
                  <Typography variant="h4" fontWeight="bold" sx={{ color: '#fff' }}>
                    {roommate.firstName}{age ? `, ${age}` : ''}
                    {roommate.gender !== undefined && roommate.gender !== 0 && (
                      <Typography component="span" sx={{ color: 'rgba(255,255,255,0.6)', fontSize: '1rem', ml: 1 }}>
                        · {GENDER_LABELS[roommate.gender as number]}
                      </Typography>
                    )}
                  </Typography>
                  {roommate.profession && (
                    <Typography variant="h6" sx={{ color: 'rgba(255,255,255,0.8)', fontWeight: 400 }}>
                      {roommate.profession}
                    </Typography>
                  )}
                  <Box sx={{ display: 'flex', gap: 2, mt: 1, flexWrap: 'wrap' }}>
                    {roommate.preferredLocation && (
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: 'rgba(255,255,255,0.7)' }}>
                        <LocationIcon sx={{ fontSize: 16 }} />
                        <Typography variant="body2">{roommate.preferredLocation}</Typography>
                      </Box>
                    )}
                    {roommate.lifestyle && (
                      <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.7)' }}>
                        {LIFESTYLE_ICONS[roommate.lifestyle]} {LIFESTYLE_LABELS[roommate.lifestyle]}
                      </Typography>
                    )}
                    {roommate.workSchedule !== undefined && (
                      <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.7)' }}>
                        {SCHEDULE_ICONS[roommate.workSchedule as number]} {SCHEDULE_LABELS[roommate.workSchedule as number]}
                      </Typography>
                    )}
                  </Box>
                </Box>
              </Box>
            </Box>

            {/* Key info bar */}
            <Box sx={{ display: 'flex', px: 3, py: 1.5, bgcolor: 'primary.main', gap: 3, flexWrap: 'wrap' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8, color: '#fff' }}>
                <CalIcon sx={{ fontSize: 18 }} />
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.7, display: 'block', lineHeight: 1 }}>Slobodan/na od</Typography>
                  <Typography variant="body2" fontWeight="bold">
                    {availableFrom === 'Odmah' ? <span style={{ color: '#a5d6a7' }}>✓ Odmah slobodan/na</span> : availableFrom}
                  </Typography>
                </Box>
              </Box>
              {(roommate.budgetMin || roommate.budgetMax) && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8, color: '#fff' }}>
                  <EuroIcon sx={{ fontSize: 18 }} />
                  <Box>
                    <Typography variant="caption" sx={{ opacity: 0.7, display: 'block', lineHeight: 1 }}>Budžet</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {roommate.budgetMin && roommate.budgetMax
                        ? `€${roommate.budgetMin} – €${roommate.budgetMax}/mj`
                        : roommate.budgetMin ? `od €${roommate.budgetMin}/mj` : `do €${roommate.budgetMax}/mj`}
                    </Typography>
                  </Box>
                </Box>
              )}
              {(roommate.minimumStayMonths || roommate.maximumStayMonths) && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8, color: '#fff' }}>
                  <Box>
                    <Typography variant="caption" sx={{ opacity: 0.7, display: 'block', lineHeight: 1 }}>Period boravka</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {roommate.minimumStayMonths && roommate.maximumStayMonths
                        ? `${roommate.minimumStayMonths}–${roommate.maximumStayMonths} mj`
                        : roommate.minimumStayMonths ? `min. ${roommate.minimumStayMonths} mj` : `max. ${roommate.maximumStayMonths} mj`}
                    </Typography>
                  </Box>
                </Box>
              )}
            </Box>
          </Paper>

          {/* About */}
          {roommate.bio && (
            <Paper sx={{ p: 3, mb: 3, borderRadius: 3 }}>
              <Typography variant="h6" fontWeight="bold" gutterBottom>O sebi</Typography>
              <Typography variant="body1" sx={{ lineHeight: 1.8, whiteSpace: 'pre-line' }}>
                {roommate.bio}
              </Typography>
            </Paper>
          )}

          {/* Hobbies + Languages */}
          {(hobbies.length > 0 || languages.length > 0) && (
            <Paper sx={{ p: 3, mb: 3, borderRadius: 3 }}>
              {hobbies.length > 0 && (
                <>
                  <Typography variant="subtitle1" fontWeight="bold" gutterBottom>Hobiji i interesovanja</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: languages.length > 0 ? 2 : 0 }}>
                    {hobbies.map(h => <Chip key={h} label={h} size="small" variant="outlined" />)}
                  </Box>
                </>
              )}
              {languages.length > 0 && (
                <>
                  <Typography variant="subtitle1" fontWeight="bold" gutterBottom>Jezici</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {languages.map(l => <Chip key={l} label={`🌐 ${l}`} size="small" />)}
                  </Box>
                </>
              )}
            </Paper>
          )}

          {/* Lifestyle preferences */}
          <Paper sx={{ p: 3, mb: 3, borderRadius: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>Stil života i pravila</Typography>
            <Grid container spacing={0}>
              <Grid item xs={12} sm={6}>
                <PrefRow label="🚬 Pušenje" value={roommate.smokingAllowed} trueLabel="Pušač" falseLabel="Nepušač" />
                <PrefRow label="🐾 Ljubimci" value={roommate.petFriendly} trueLabel="Da, OK" falseLabel="Ne" />
                <PrefRow label="👥 Gosti" value={roommate.guestsAllowed} trueLabel="Da, OK" falseLabel="Ne" />
                <PrefRow label="🎵 Muzika/instrumenti" value={roommate.musicFriendly} trueLabel="Da, OK" falseLabel="Ne" />
              </Grid>
              <Grid item xs={12} sm={6}>
                {roommate.cleanliness && (
                  <PrefRow label="🧹 Urednost" value={CLEANLINESS_LABELS[roommate.cleanliness] || roommate.cleanliness} />
                )}
                {roommate.lifestyle && (
                  <PrefRow label="💫 Tip stanara" value={`${LIFESTYLE_ICONS[roommate.lifestyle]} ${LIFESTYLE_LABELS[roommate.lifestyle]}`} />
                )}
                {roommate.workSchedule !== undefined && (
                  <PrefRow label="⏰ Ritam dana" value={`${SCHEDULE_ICONS[roommate.workSchedule as number]} ${SCHEDULE_LABELS[roommate.workSchedule as number]}`} />
                )}
              </Grid>
            </Grid>
          </Paper>

          {/* What they're looking for */}
          {(roommate.lookingForRoomType || roommate.lookingForApartmentType || roommate.preferredLocation) && (
            <Paper sx={{ p: 3, borderRadius: 3 }}>
              <Typography variant="h6" fontWeight="bold" gutterBottom>Šta traži</Typography>
              {roommate.preferredLocation && (
                <Box sx={{ display: 'flex', gap: 0.8, alignItems: 'center', mb: 1 }}>
                  <LocationIcon color="action" sx={{ fontSize: 18 }} />
                  <Typography variant="body1">{roommate.preferredLocation}</Typography>
                </Box>
              )}
              {roommate.lookingForRoomType && (
                <Box sx={{ mb: 0.5 }}>
                  <Typography component="span" variant="body2" color="text.secondary">Tip: </Typography>
                  <Chip label={roommate.lookingForRoomType} size="small" />
                </Box>
              )}
            </Paper>
          )}
        </Grid>

        {/* ── Right column ─────────────────────────────────────────────── */}
        <Grid item xs={12} md={4}>
          <Box sx={{ position: 'sticky', top: 90, display: 'flex', flexDirection: 'column', gap: 2 }}>

            {/* Contact card */}
            <Card sx={{ borderRadius: 3 }}>
              <CardContent>
                {!isOwn ? (
                  <>
                    <Button fullWidth variant="contained" color="secondary" size="large"
                      onClick={() => navigate(`/chat?userId=${roommate.userId}`)} sx={{ mb: 1.5, borderRadius: 2 }}>
                      💬 Pošalji poruku
                    </Button>
                    <Typography variant="caption" color="text.secondary" display="block" textAlign="center">
                      Predstavi se kratko — veće šanse za odgovor
                    </Typography>
                  </>
                ) : (
                  <>
                    <Typography variant="subtitle2" fontWeight="bold" gutterBottom>Kompletnost profila</Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                      <Typography variant="body2" color="text.secondary">Popunjenost</Typography>
                      <Typography variant="body2" fontWeight="bold"
                        color={completeness >= 80 ? 'success.main' : 'warning.main'}>{completeness}%</Typography>
                    </Box>
                    <LinearProgress variant="determinate" value={completeness}
                      color={completeness >= 80 ? 'success' : 'warning'}
                      sx={{ borderRadius: 4, height: 8, mb: 2 }} />
                    {completeness < 80 && (
                      <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 2 }}>
                        Dopuni profil da povećaš vidljivost i dobiješ više upita.
                      </Typography>
                    )}
                    <Button fullWidth variant="contained" startIcon={<EditIcon />}
                      onClick={() => navigate('/roommates/create')} sx={{ mb: 1, borderRadius: 2 }}>
                      Uredi profil
                    </Button>
                    <Button fullWidth variant="outlined" color="error" startIcon={<DeleteIcon />}
                      onClick={() => setDeleteOpen(true)} sx={{ borderRadius: 2 }}>
                      Obriši profil
                    </Button>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Quick stats */}
            <Card sx={{ borderRadius: 3 }}>
              <CardContent>
                <Typography variant="subtitle2" fontWeight="bold" gutterBottom>Brzi pregled</Typography>
                <Divider sx={{ mb: 1.5 }} />
                {availableFrom !== 'Odmah' && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.6 }}>
                    <Typography variant="body2" color="text.secondary">Slobodan/na od</Typography>
                    <Typography variant="body2" fontWeight="medium">{availableFrom}</Typography>
                  </Box>
                )}
                {availableFrom === 'Odmah' && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.6 }}>
                    <Typography variant="body2" color="text.secondary">Useljenje</Typography>
                    <Typography variant="body2" fontWeight="medium" color="success.main">✓ Odmah slobodan/na</Typography>
                  </Box>
                )}
                {roommate.budgetIncludes && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.6 }}>
                    <Typography variant="body2" color="text.secondary">Uključeno u cenu</Typography>
                    <Typography variant="body2" fontWeight="medium" sx={{ maxWidth: 130, textAlign: 'right' }}>
                      {roommate.budgetIncludes}
                    </Typography>
                  </Box>
                )}
                {roommate.minimumStayMonths && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.6 }}>
                    <Typography variant="body2" color="text.secondary">Min. period</Typography>
                    <Typography variant="body2" fontWeight="medium">{roommate.minimumStayMonths} mj</Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Box>
        </Grid>
      </Grid>

      {/* Delete dialog */}
      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Obriši cimerski profil</DialogTitle>
        <DialogContent>
          <Typography>Da li si siguran/na da želiš obrisati profil? Ova akcija se ne može poništiti.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Odustani</Button>
          <Button color="error" variant="contained" onClick={() => deleteMutation.mutate()}
            disabled={deleteMutation.isPending}>
            Obriši
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default RoommateDetailPage;
