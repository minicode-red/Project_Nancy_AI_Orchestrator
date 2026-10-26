if(NOT TARGET OpenXR::headers)
add_library(OpenXR::headers INTERFACE IMPORTED)
set_target_properties(OpenXR::headers PROPERTIES
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/AdministratorMPtech/.gradle/caches/8.11/transforms/c92697ca7bc1e2f3d44592dac6c5c0d8/transformed/jetified-openxr_loader/prefab/modules/headers/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET OpenXR::openxr_loader)
add_library(OpenXR::openxr_loader SHARED IMPORTED)
set_target_properties(OpenXR::openxr_loader PROPERTIES
    IMPORTED_LOCATION "C:/Users/AdministratorMPtech/.gradle/caches/8.11/transforms/c92697ca7bc1e2f3d44592dac6c5c0d8/transformed/jetified-openxr_loader/prefab/modules/openxr_loader/libs/android.arm64-v8a/libopenxr_loader.so"
    INTERFACE_LINK_LIBRARIES "OpenXR::headers"
)
endif()

