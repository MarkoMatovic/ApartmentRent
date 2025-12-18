// Auth types matching backend DTOs

export interface UserRegistrationDto {
  firstName: string;
  email: string;
}

export interface UserRegistrationInputDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  dateOfBirth: string; // ISO date string
  phoneNumber: string;
  profilePicture?: string;
  createdDate: string; // ISO date string
}

export interface LoginUserInputDto {
  email: string;
  password: string;
}

export interface ChangePasswordInputDto {
  userId: string; // GUID
  oldPassword: string;
  newPassword: string;
}

export interface DeactivateUserInputDto {
  userGuid: string; // GUID
}

export interface ReactivateUserInputDto {
  userGuid: string; // GUID
}

export interface DeleteUserInputDto {
  userGuid: string; // GUID
}

// Login response is a JWT token string
export type LoginResponse = string;

